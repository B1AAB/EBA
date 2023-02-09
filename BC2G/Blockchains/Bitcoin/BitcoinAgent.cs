// BitcoinAgent and all similar agents must implement co-operative cancellation semantics:
// https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads?redirectedfrom=MSDN

using BC2G.Model;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BC2G.Blockchains;

public class BitcoinAgent : IDisposable
{
    public const string Coinbase = "Coinbase";
    public const uint GenesisTimestamp = 1231006505;

    private readonly HttpClient _client;
    private readonly ILogger<BitcoinAgent> _logger;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    private bool _disposed = false;

    public BitcoinAgent(
        HttpClient client,
        IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<BitcoinAgent> logger)
    {
        _client = client;
        _dbContextFactory = dbContextFactory;
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
        int height,
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
            throw;
            // The exception can happen when the given block height 
            // is invalid throw new Exception($"Invalid height {height}");
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

    public async Task<BitcoinBlockGraph> GetGraph(
        int height,
        ConcurrentDictionary<string, Utxo> utxos, 
        object dbContextLock, 
        CancellationToken cT)
    {
        // All the logging in this section are removed because 
        // CPU profiling shows ~%24 of the process time is spent on them.

        cT.ThrowIfCancellationRequested();

        var blockHash = await GetBlockHashAsync(height, cT);

        cT.ThrowIfCancellationRequested();

        var block = await GetBlockAsync(blockHash, cT);

        cT.ThrowIfCancellationRequested();

        var graph = await ProcessBlockAsync(block, utxos, dbContextLock, cT);
        return graph;
    }

    private async Task<BitcoinBlockGraph> ProcessBlockAsync(
        Block block, 
        ConcurrentDictionary<string, Utxo> utxos,
        object dbContextLock, 
        CancellationToken cT)
    {
        // By definition, each block has a generative block that is the
        // reward of the miner. Hence, this should never raise an 
        // exception if the block is not corrupt.
        var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);

        var generationTxGraph = new TransactionGraph(coinbaseTx);

        var g = new BitcoinBlockGraph(block, generationTxGraph);

        var rewardAddresses = new List<ScriptNode>();
        foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
        {
            output.TryGetAddress(out string address);
            var node = generationTxGraph.AddTarget(
                Utxo.GetId(coinbaseTx.Txid, output.Index),
                address, output.GetScriptType(), output.Value);

            rewardAddresses.Add(node);
            g.Stats.AddInputTxCount(1);

            var utxo = new Utxo(
                coinbaseTx.Txid,
                output.Index,
                address,
                output.Value,
                block.Hash);

            utxos.AddOrUpdate(
                utxo.Id, utxo,
                (k, oldValue) =>
                {
                    oldValue.AddCreatedIn(block.Height.ToString());
                    return oldValue;
                });
        }

        cT.ThrowIfCancellationRequested();

        g.RewardsAddresses = rewardAddresses;

        var dbContext = _dbContextFactory.CreateDbContext();
        var options = new ParallelOptions()
        {
            CancellationToken = cT,
            #if (DEBUG)
            MaxDegreeOfParallelism = 1
            #endif
        };
        await Parallel.ForEachAsync(
            block.Transactions.Where(x => !x.IsCoinbase),
            options,
            async (tx, _loopCancellationToken) =>
            {
                _loopCancellationToken.ThrowIfCancellationRequested();
                await ProcessTx(g, tx, utxos, dbContext, dbContextLock, cT);
            });

        
        cT.ThrowIfCancellationRequested();
        dbContext.Dispose();

        return g;
    }

    private async Task ProcessTx(
        BitcoinBlockGraph g,
        Transaction tx,
        ConcurrentDictionary<string, Utxo> utxos,
        DatabaseContext dbContext,
        object dbContextLock,
        CancellationToken cT)
    {
        var txGraph = new TransactionGraph(tx) { Fee = tx.Fee };

        foreach (var input in tx.Inputs)
        {
            cT.ThrowIfCancellationRequested();

            double value = 0;
            string address = string.Empty;

            var id = Utxo.GetId(input.TxId, input.OutputIndex);

            // This tries to find the output that the given input references, 
            // it performs it in the following order: 
            // - search among the newly created but not in the db instances,
            //   if found, update accordingly;
            // - search db, if found, update it and save changes---all under
            //   a lock synchronized through-out the program;
            // - query the bitcoin client, and add the determined output 
            //   to the utxo dict so it will be persisted in the db. 
            if (utxos.TryGetValue(id, out Utxo? utxo))
            {
                value = utxo.Value;
                address = utxo.Address;
                utxo.AddReferencedIn(g.Block.Hash);
            }
            else
            {
                lock (dbContextLock)
                {
                    utxo = dbContext.Utxos.Find(id);
                    if (utxo != null)
                    {
                        value = utxo.Value;
                        address = utxo.Address;
                        utxo.AddReferencedIn(g.Block.Hash);

                        // This invalidates the ACID property since if the
                        // block process is canceled before it completes, 
                        // some related changes are already saved in the db. 
                        dbContext.SaveChanges();
                    }
                }

                if (utxo == null)
                {
                    // Extended transaction: details of the transaction are
                    // retrieved from the bitcoin client.
                    var exTx = await GetTransactionAsync(input.TxId, cT);
                    var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                    if (vout == null)
                        throw new NotImplementedException($"{vout} not in {input.TxId}; not expected.");

                    vout.TryGetAddress(out address);
                    value = vout.Value;

                    var cIn = exTx.BlockHash;
                    var rIn = g.Block.Hash;
                    utxo = new Utxo(id, address, value, cIn, rIn);

                    utxos.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                    {
                        oldValue.AddCreatedIn(cIn);
                        oldValue.AddReferencedIn(rIn);
                        return oldValue;
                    });
                }
            }

            txGraph.AddSource(input.TxId, utxo.Id, address, ScriptType.Unknown, value);
            // TODO: Any better value instead of ScriptType.Unknown?
        }

        foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
        {
            cT.ThrowIfCancellationRequested();

            output.TryGetAddress(out string address);
            txGraph.AddTarget(Utxo.GetId(tx.Txid, output.Index), address, output.GetScriptType(), output.Value);

            var cIn = g.Block.Hash;
            var utxo = new Utxo(tx.Txid, output.Index, address, output.Value, cIn);

            utxos.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
            {
                oldValue.AddCreatedIn(cIn);
                return oldValue;
            });
        }

        g.Stats.AddInputTxCount(tx.Inputs.Count);
        g.Stats.AddOutputTxCount(tx.Outputs.Count);
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
