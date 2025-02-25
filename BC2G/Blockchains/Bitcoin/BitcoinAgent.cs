namespace BC2G.Blockchains.Bitcoin;

public class BitcoinAgent : IDisposable
{
    public const string Coinbase = "Coinbase";
    public const uint GenesisTimestamp = 1231006505;

    /// <summary>
    /// The amount of satoshis in one BTC.
    /// Based-on: https://github.com/bitcoin/bitcoin/blob/35bf426e02210c1bbb04926f4ca2e0285fbfcd11/src/consensus/amount.h#L15
    /// </summary>
    public const ulong Coin = 100000000;

    private readonly HttpClient _client;
    private readonly ILogger<BitcoinAgent> _logger;

    private bool _disposed = false;

    public BitcoinAgent(
        HttpClient client,
        ILogger<BitcoinAgent> logger)
    {
        _client = client;
        _logger = logger;
    }

    private async Task<Stream> GetResourceAsync(
        string endpoint,
        string hash,
        CancellationToken cT)
    {
        return await GetStreamAsync($"{endpoint}/{hash}.json", cT);
    }

    private async Task<Stream> GetStreamAsync(
        string endpoint,
        CancellationToken cT)
    {
        var response = await _client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cT);
        return await response.Content.ReadAsStreamAsync(cT);
    }

    /// <summary>
    /// Is true if it can successfully query the `chaininfo` endpoint of 
    /// the Bitcoin client, false if otherwise.
    /// 
    /// This will break as soon as the circuit breaker breaks the circuit
    /// for the first time; hence, will not retry if the circuit returns
    /// half-open or is reseted.
    /// </summary>
    public async Task<(bool, ChainInfo?)> IsConnectedAsync(
        CancellationToken cT)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, "chaininfo.json");

            request.SetPolicyExecutionContext(
                new Context().SetLogger<BitcoinAgent>(_logger));

            var response = await _client.SendAsync(request, cT);
            response.EnsureSuccessStatusCode();

            var chainInfo = await JsonSerializer.DeserializeAsync<ChainInfo>(
                await response.Content.ReadAsStreamAsync(cT),
                cancellationToken: cT);

            return (response.IsSuccessStatusCode, chainInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Failed to communicate with the Bitcoin client at {clientBaseAddress}. " +
                "Double-check if the client is running and listening " +
                "at the given endpoint and port. Also, make sure the " +
                "client is started with the REST endpoint enabled " +
                "(see the docs). {exception}", // TODO: add link to related docs. 
                $"{_client.BaseAddress}/chaininfo.json",
                ex.Message);
            return (false, null);
        }
    }

    public async Task<ChainInfo> AssertChainAsync(
        CancellationToken cT)
    {
        _logger.LogInformation(
            "Checking if can communicate with Bitcoin-qt, " +
            "and getting Bitcoin chain information.");

        (var isConnected, var chainInfo) = await IsConnectedAsync(cT);

        if (!isConnected || chainInfo is null)
            throw new Exception(
                $"Failed to communicate with the Bitcoin client at {_client.BaseAddress}. " +
                "Double-check if the client is running and listening " +
                "at the given endpoint and port. Also, make sure the " +
                "client is started with the REST endpoint enabled " +
                "(see the docs)."); // TODO: add link to related docs.

        if (chainInfo.Chain != "main")
            throw new Exception(
                $"Required to be on the `main` chain, " +
                $"but the bitcoin client is on the " +
                $"`{chainInfo.Chain}` chain.");

        _logger.LogInformation(
            "Successfully communicated with Bitcoin-qt, " +
            "and received chain information.");

        return chainInfo;
    }

    public async Task<string> GetBlockHashAsync(
        long height,
        CancellationToken cT)
    {
        try
        {
            await using var stream = await GetStreamAsync($"blockhashbyheight/{height}.hex", cT);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Trim();
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed getting block hash for block {h:n0}; " +
                "this exception can happen when the given block height is invalid. {e}",
                height, e.Message);
            throw;
        }
    }

    public async Task<Block> GetBlockAsync(
        string hash,
        CancellationToken cT)
    {
        await using var stream = await GetResourceAsync("block", hash, cT);
        return
            await JsonSerializer.DeserializeAsync<Block>(
                stream, cancellationToken: cT)
            ?? throw new Exception("Invalid block.");
    }

    public async Task<Transaction> GetTransactionAsync(
        string hash,
        CancellationToken cT)
    {
        await using var stream = await GetResourceAsync("tx", hash, cT);
        return
            await JsonSerializer.DeserializeAsync<Transaction>(stream, cancellationToken: cT)
            ?? throw new Exception("Invalid transaction.");
    }

    public async Task<BlockGraph?> GetGraph(
        long height,
        IAsyncPolicy strategy,
        BitcoinOptions options,
        CancellationToken cT)
    {
        BlockGraph? graph = null;
        int retryAttempts = 0;

        try
        {
            await strategy.ExecuteAsync(
                async (context, cT) =>
                {
                    retryAttempts++;
                    graph = await GetGraph(height, options, cT);
                    graph.Stats.Retries = retryAttempts;
                },
                new Context()
                    .SetLogger<Orchestrator>(_logger)
                    .SetBlockHeight(height),
                cT);
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException e)
        {
            _logger.LogError(
                "Circuit is broken processing block {h:n0}! {e}.",
                height, e.Message);
        }

        return graph;
    }

    public async Task<BlockGraph> GetGraph(
        long height, 
        BitcoinOptions options,
        CancellationToken cT)
    {
        cT.ThrowIfCancellationRequested();

        var blockHash = await GetBlockHashAsync(height, cT);

        cT.ThrowIfCancellationRequested();

        var block = await GetBlockAsync(blockHash, cT);

        cT.ThrowIfCancellationRequested();

        var graph = await ProcessBlockAsync(block, options, cT);

        graph.MergeQueuedTxGraphs(cT);

        graph.Stats.StopStopwatch();

        return graph;
    }

    private async Task<BlockGraph> ProcessBlockAsync(
        Block block,
        BitcoinOptions options,
        CancellationToken cT)
    {
        // By definition, each block has a generative block that is the
        // reward of the miner. Hence, this should never raise an 
        // exception if the block is not corrupt.
        var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);

        var mintingTxGraph = new TransactionGraph(coinbaseTx);

        var g = new BlockGraph(block, mintingTxGraph, _logger);

        var rewardAddresses = new List<ScriptNode>();
        foreach (var output in coinbaseTx.Outputs)
        {
            if (!output.IsValueTransfer)
            {
                // This is added for script type statistics only
                g.Stats.AddNonTransferOutputStatistics(output.GetScriptType());
                continue;
            }

            output.TryGetAddress(out string? address);

            g.Stats.AddOutputStatistics(address, output.GetScriptType());

            var utxo = new Utxo(
                coinbaseTx.Txid, output.Index, address, output.Value, output.GetScriptType(),
                isGenerated: true, createdInHeight: block.Height);

            if (options.TrackTxo)
                g.Block.TxoLifecycle.AddOrUpdate(utxo.Id, utxo,
                    (k, oldValue) =>
                    {
                        oldValue.AddCreatedIn(block.Height);
                        return oldValue;
                    });

            var node = mintingTxGraph.AddTarget(utxo);
            rewardAddresses.Add(node);
        }

        g.Stats.CoinbaseOutputsCount = rewardAddresses.Count;

        cT.ThrowIfCancellationRequested();

        g.RewardsAddresses = rewardAddresses;

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cT,
            #if (DEBUG)
            MaxDegreeOfParallelism = 1
            #endif
        };
        await Parallel.ForEachAsync(
            block.Transactions.Where(x => !x.IsCoinbase),
            parallelOptions,
            async (tx, _loopCancellationToken) =>
            {
                _loopCancellationToken.ThrowIfCancellationRequested();
                await ProcessTx(g, tx, options, cT);
            });

        cT.ThrowIfCancellationRequested();

        return g;
    }

    private async Task ProcessTx(BlockGraph g, Transaction tx, BitcoinOptions options, CancellationToken cT)
    {
        var txGraph = new TransactionGraph(tx) { Fee = tx.Fee };

        foreach (var input in tx.Inputs)
        {
            cT.ThrowIfCancellationRequested();

            var id = Utxo.GetId(input.TxId, input.OutputIndex);

            Utxo? utxo = null;

            if (input.PrevOut != null)
            {
                input.PrevOut.ConstructedOutput.TryGetAddress(out string? address);

                utxo = new Utxo(
                    txid: input.TxId,
                    voutN: input.OutputIndex,
                    address: address,
                    value: input.PrevOut.Value,
                    isGenerated: input.PrevOut.Generated,
                    scriptType: input.PrevOut.ConstructedOutput.GetScriptType(),
                    createdInHeight: input.PrevOut.Height,
                    spentInHeight: g.Block.Height);

                if (options.TrackTxo)
                    g.Block.TxoLifecycle.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                    {
                        oldValue.AddCreatedIn(height: input.PrevOut.Height);
                        oldValue.AddSpentIn(g.Block.Height);
                        return oldValue;
                    });

                g.Stats.AddSpentOutputsAge((int)(g.Block.Height - input.PrevOut.Height));
            }
            else
            {
                throw new NotImplementedException($"This is not expected. Block = {g.Block.Height}");

                // Extended transaction: details of the transaction are
                // retrieved from the bitcoin client.
                /*var exTx = await GetTransactionAsync(input.TxId, cT);
                var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                if (vout == null)
                    throw new NotImplementedException($"{vout} not in {input.TxId}; not expected.");

                vout.TryGetAddress(out string? address);

                var cIn = exTx.BlockHash;
                utxo = new Utxo(
                    id, address, vout.Value, vout.GetScriptType(),
                    createdInBlockHash: cIn, spentInBlockHeight: g.Block.Height.ToString());

                utxos.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                {
                    oldValue.AddCreatedIn(height: string.Empty, cIn);
                    oldValue.AddSpentIn(g.Block.Height.ToString());
                    return oldValue;
                });*/
            }

            g.Stats.AddInputValue(utxo.Value);
            txGraph.AddSource(input.TxId, utxo);
        }

        var transferOutputsCount = 0;
        foreach (var output in tx.Outputs)
        {
            cT.ThrowIfCancellationRequested();

            if (!output.IsValueTransfer)
            {
                // This is added for script type statistics only
                g.Stats.AddNonTransferOutputStatistics(output.GetScriptType());
                continue;
            }

            transferOutputsCount++;

            output.TryGetAddress(out string? address);
            g.Stats.AddOutputStatistics(address, output.GetScriptType());

            var cIn = g.Block.Hash;
            var utxo = new Utxo(
                tx.Txid, output.Index, address, output.Value, output.GetScriptType(),
                isGenerated: false, createdInHeight: g.Block.Height);

            txGraph.AddTarget(utxo);
            g.Stats.AddOutputValue(utxo.Value);

            if (options.TrackTxo)
                g.Block.TxoLifecycle.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                {
                    oldValue.AddCreatedIn(g.Block.Height);
                    return oldValue;
                });
        }

        g.Stats.AddInputsCount(tx.Inputs.Count);
        g.Stats.AddOutputsCount(transferOutputsCount);
        g.Enqueue(txGraph);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(
        bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //_txCache.Dispose();
            }

            _disposed = true;
        }
    }
}
