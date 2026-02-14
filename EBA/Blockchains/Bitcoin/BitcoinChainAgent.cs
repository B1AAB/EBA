using EBA.Graph.Bitcoin;

namespace EBA.Blockchains.Bitcoin;

public class BitcoinChainAgent : IDisposable
{
    public const NodeLabels Coinbase = NodeLabels.Coinbase;
    public const uint GenesisTimestamp = 1231006505;

    /// <summary>
    /// The amount of satoshis in one BTC.
    /// Based-on: https://github.com/bitcoin/bitcoin/blob/35bf426e02210c1bbb04926f4ca2e0285fbfcd11/src/consensus/amount.h#L15
    /// </summary>
    public const ulong Coin = 100000000;

    private readonly HttpClient _client;
    private readonly ILogger<BitcoinChainAgent> _logger;

    private bool _disposed = false;

    public BitcoinChainAgent(
        HttpClient client,
        ILogger<BitcoinChainAgent> logger)
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
                new Context().SetLogger<BitcoinChainAgent>(_logger));

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
            "Checking if can communicate with Bitcoin Core, " +
            "and getting Bitcoin chain information.");

        (var isConnected, var chainInfo) = await IsConnectedAsync(cT);

        if (!isConnected || chainInfo is null)
            throw new Exception(
                $"Failed to communicate with the Bitcoin client at {_client.BaseAddress}. " +
                "Double-check if the client is running and listening " +
                "at the given endpoint and port. Also, make sure the " +
                "client is started with the REST endpoint enabled " +
                "(see https://eba.b1aab.ai/docs/bitcoin/etl/node-sync).");

        if (chainInfo.Chain != "main")
            throw new Exception(
                $"Required to be on the `main` chain, " +
                $"but the bitcoin client is on the " +
                $"`{chainInfo.Chain}` chain.");

        _logger.LogInformation(
            "Successfully communicated with Bitcoin Core, " +
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

    public async Task<Tx> GetTransactionAsync(
        string hash,
        CancellationToken cT)
    {
        await using var stream = await GetResourceAsync("tx", hash, cT);
        return
            await JsonSerializer.DeserializeAsync<Tx>(stream, cancellationToken: cT)
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
                    graph.Retries = retryAttempts;
                },
                new Context()
                    .SetLogger<BitcoinOrchestrator>(_logger)
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

        graph.StopStopwatch();

        return graph;
    }

    private async Task<BlockGraph> ProcessBlockAsync(
        Block block,
        BitcoinOptions options,
        CancellationToken cT)
    {
        var g = new BlockGraph(block, ChainToGraphModel.UTxOModel, _logger);

        // By definition, each block has a generative block that is the
        // reward of the miner. Hence, this should never raise an 
        // exception if the block is not corrupt.
        var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);        

        var mintingTxGraph = new TxGraph(coinbaseTx);
        foreach (var output in coinbaseTx.Outputs)
        {
            mintingTxGraph.AddOutput(output);

            if (options.Traverse.TrackTxo)
            {
                output.TryGetAddress(out string? address);
                var utxo = new Utxo(
                    output.ScriptPubKey,
                    address,
                    output.Value,
                    output.ScriptPubKey.ScriptType,
                    isGenerated: true,
                    createdInHeight: block.Height);
                g.Block.TxoLifecycle.TryAdd(utxo.Id, utxo);
            }
        }

        g.SetCoinbaseTx(mintingTxGraph);

        cT.ThrowIfCancellationRequested();

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

        g.BuildGraph(cT);

        return g;
    }

    private static async Task ProcessTx(BlockGraph g, Tx tx, BitcoinOptions options, CancellationToken cT)
    {
        var txGraph = new TxGraph(tx) { Fee = tx.Fee };

        foreach (var input in tx.Inputs)
        {
            cT.ThrowIfCancellationRequested();

            if (input.PrevOut == null)
                throw new NotImplementedException(
                    $"Unexpected null {nameof(input.PrevOut)}; Block = {g.Block.Height}");

            txGraph.AddInput(input);

            if (options.Traverse.TrackTxo)
            {
                var prevOut = input.PrevOut.ConstructedOutput;
                prevOut.TryGetAddress(out string? address);

                var utxo = new Utxo(
                    input.PrevOut.ScriptPubKey,
                    address: address,
                    value: input.PrevOut.Value,
                    isGenerated: input.PrevOut.Generated,
                    scriptType: input.PrevOut.ConstructedOutput.ScriptPubKey.ScriptType,
                    createdInHeight: input.PrevOut.Height,
                    spentInHeight: g.Block.Height);

                g.Block.TxoLifecycle.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                {
                    oldValue.SpentInBlockHeight = g.Block.Height;
                    return oldValue;
                });
            }
        }

        foreach (var output in tx.Outputs)
        {
            cT.ThrowIfCancellationRequested();

            txGraph.AddOutput(output);

            if (output.ScriptPubKey.ScriptType != ScriptType.NullData &&
                output.ScriptPubKey.ScriptType != ScriptType.nonstandard &&
                options.Traverse.TrackTxo)
            {
                output.TryGetAddress(out string? address);

                var utxo = new Utxo(
                    id: Utxo.GetId(txGraph.TxNode.Txid, output.Index),
                    address: address,
                    value: output.Value,
                    scriptType: output.ScriptPubKey.ScriptType,
                    isGenerated: false,
                    createdInBlockHeight: g.Block.Height);

                g.Block.TxoLifecycle.TryAdd(utxo.Id, utxo);
            }
        }

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
