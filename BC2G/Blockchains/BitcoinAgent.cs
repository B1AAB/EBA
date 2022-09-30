using BC2G.Graph;
using BC2G.Infrastructure;
using BC2G.Model;
using BC2G.StartupSolutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using System.Collections.Concurrent;
using System.Text.Json;

// BitcoinAgent and all similar agents must implement co-operative cancellation semantics:
// https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads?redirectedfrom=MSDN

namespace BC2G.Blockchains
{
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

        private async Task<Stream> GetResourceAsync(string endpoint, string hash, CancellationToken cT)
        {
            return await GetStreamAsync($"{endpoint}/{hash}.json", cT);
        }

        private async Task<Stream> GetStreamAsync(string endpoint, CancellationToken cT)
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
        public async Task<(bool, ChainInfo?)> IsConnectedAsync(CancellationToken cT)
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

        public async Task<ChainInfo> AssertChainAsync(CancellationToken cT)
        {
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

            return chainInfo;
        }

        public async Task<string> GetBlockHashAsync(int height, CancellationToken cT)
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

        public async Task<Block> GetBlockAsync(string hash, CancellationToken cT)
        {
            await using var stream = await GetResourceAsync("block", hash, cT);
            return
                await JsonSerializer.DeserializeAsync<Block>(
                    stream, cancellationToken: cT)
                ?? throw new Exception("Invalid block.");
        }

        public async Task<Transaction> GetTransactionAsync(string hash, CancellationToken cT)
        {
            await using var stream = await GetResourceAsync("tx", hash, cT);
            return
                await JsonSerializer.DeserializeAsync<Transaction>(stream, cancellationToken: cT)
                ?? throw new Exception("Invalid transaction.");
        }

        public async Task<BlockGraph> GetGraph(int height, CancellationToken cT)
        {
            // All the logging in this section are removed because 
            // CPU profiling shows ~%24 of the process time is spent on them.

            cT.ThrowIfCancellationRequested();

            var blockHash = await GetBlockHashAsync(height, cT);

            cT.ThrowIfCancellationRequested();

            var block = await GetBlockAsync(blockHash, cT);

            cT.ThrowIfCancellationRequested();

            var graph = new BlockGraph(block);
            await ProcessTxesAsync(graph, block, cT);

            return graph;
        }

        private async Task ProcessTxesAsync(BlockGraph g, Block block, CancellationToken cT)
        {
            var utxos = new ConcurrentDictionary<string, Utxo>();

            var generationTxGraph = new TransactionGraph();

            // By definition, each block has a generative block that is the
            // reward of the miner. Hence, this should never raise an 
            // exception if the block is not corrupt.
            var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);

            var rewardAddresses = new List<Node>();
            foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
            {
                output.TryGetAddress(out string address);
                var node = generationTxGraph.AddTarget(
                    new Node(address, output.GetScriptType()),
                    output.Value);

                rewardAddresses.Add(node);
                g.Stats.AddInputTxCount(1);

                var utxo = new Utxo(
                    coinbaseTx.Txid,
                    output.Index,
                    address,
                    output.Value,
                    block.Height.ToString())
                { CreatedInCount = 1 };

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
            g.Enqueue(generationTxGraph);

            var dbContextLock = new object();
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

            await dbContext.SaveChangesAsync(cT);
            dbContext.Dispose();

            cT.ThrowIfCancellationRequested();
            await AddOrUpdateRange(utxos.Values, cT);
        }

        private async Task ProcessTx(
            BlockGraph g,
            Transaction tx,
            ConcurrentDictionary<string, Utxo> utxos,
            DatabaseContext dbContext,
            object dbContextLock,
            CancellationToken cT)
        {
            var txGraph = new TransactionGraph { Fee = tx.Fee };

            foreach (var input in tx.Inputs)
            {
                cT.ThrowIfCancellationRequested();

                double value;
                string address;

                // Creating a separate context for each operation is not ideal, though
                // EF does not currently support concurrency on a context. Therefore, 
                // a context cannot be re-used on multi-thread setup. 
                //using var context = _dbContextFactory.CreateDbContext();

                // Do NOT pass the cancelation token to following FindAsync, it seems there are 
                // known complications related to this: https://github.com/dotnet/efcore/issues/12012
                // #pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
                // var utxo = await context.Utxos.FindAsync(Utxo.GetId(input.TxId, input.OutputIndex));
                // #pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods

                Utxo? utxo;
                lock(dbContextLock)
                    utxo = dbContext.Utxos.Find(
                        Utxo.GetId(input.TxId, input.OutputIndex));

                if (utxo != null)
                {
                    value = utxo.Value;
                    address = utxo.Address;
                    if (!string.IsNullOrEmpty(utxo.ReferencedIn))
                        utxo.ReferencedIn += ";";
                    utxo.ReferencedIn += g.Block.Height.ToString();
                    utxo.ReferencedInCount++;
                }
                else
                {
                    // Extended transaction: details of the transaction are
                    // retrieved from the bitcoin client.
                    var exTx = await GetTransactionAsync(input.TxId, cT);
                    var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                    if (vout == null)
                        // TODO: check when this can be null,
                        // or if it would ever happen.
                        throw new NotImplementedException();

                    vout.TryGetAddress(out address);
                    value = vout.Value;

                    // TODO: it would require much less chars to serialize if 
                    // we would store block height instead of block hash. However, 
                    // for exTx that would require an additional API call to the
                    // bitcoin client, which may not be the most efficient.

                    CreateOrUpdateUtxo(
                        dbContext, dbContextLock, utxos,
                        Utxo.GetId(input.TxId, input.OutputIndex),
                        address, value, exTx.BlockHash, g.Height.ToString());
                }

                txGraph.AddSource(
                    new Node(address, ScriptType.Unknown), // TODO: can this set to a better value?
                    value);
            }

            foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
            {
                cT.ThrowIfCancellationRequested();

                output.TryGetAddress(out string address);
                txGraph.AddTarget(
                    new Node(address, output.GetScriptType()),
                    output.Value);

                CreateOrUpdateUtxo(
                    dbContext, dbContextLock, utxos,
                    Utxo.GetId(tx.Txid, output.Index),
                    address, output.Value, g.Block.Height.ToString(), string.Empty);
            }

            g.Stats.AddInputTxCount(tx.Inputs.Count);
            g.Stats.AddOutputTxCount(tx.Outputs.Count);
            g.Enqueue(txGraph);
        }

        private static void CreateOrUpdateUtxo(
            DatabaseContext dbContext,
            object dbContextLock,
            ConcurrentDictionary<string, Utxo> utxos,
            string id,
            string address,
            double value,
            string createdIn,
            string referencedIn)
        {
            lock (dbContextLock)
            {
                var trackedUtxo = dbContext.Utxos.Local.FirstOrDefault(x => x.Id == id);
                if (trackedUtxo != null)
                {
                    trackedUtxo.AddCreatedIn(createdIn);
                    if (!string.IsNullOrEmpty(referencedIn))
                        trackedUtxo.AddReferencedIn(referencedIn);
                }
                else
                {
                    var utxo = new Utxo(id, address, value, createdIn, referencedIn)
                    {
                        CreatedInCount = 1
                    };

                    if (!string.IsNullOrEmpty(referencedIn))
                        utxo.ReferencedInCount = 1;

                    utxos.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                    {
                        oldValue.AddCreatedIn(createdIn);
                        if (!string.IsNullOrEmpty(referencedIn))
                            oldValue.AddReferencedIn(referencedIn);
                        return oldValue;
                    });
                }
            }
        }

        // Based on the cpu profiling, this method takes most of the cpu time (about ~%20). 
        private async Task AddOrUpdateRange(ICollection<Utxo> utxos, CancellationToken cT)
        {
            // VERY IMPORTANT TODO:
            // This method must go through a complete re-write for simplicy and performance,
            // see the linked docs for a better implementation tips.
            // https://learn.microsoft.com/en-us/ef/core/saving/concurrency

            using var c = _dbContextFactory.CreateDbContext();

            try
            {
                await c.Utxos.AddRangeAsync(utxos, cT);
                await c.SaveChangesAsync(cT);
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case InvalidOperationException:
                    case DbUpdateConcurrencyException:
                    case DbUpdateException when (
                        e.InnerException is PostgresException pe &&
                        pe.SqlState == "23505"):
                        // A list of the error codes are available in the following page.
                        // https://www.postgresql.org/docs/current/errcodes-appendix.html
                        //
                        // - 23505: unique_violation (when adding an entity whose indexed
                        //   property is already defined).

                        // TODO:
                        // There is a much better implementation of this that can 
                        // better handle concurrency issues at the following, 
                        // this should be updated according to the example in 
                        // the following link.
                        // https://learn.microsoft.com/en-us/ef/core/saving/concurrency

                        _logger.LogInformation(
                            "The exception with the following error message is handled. " +
                            "There are a few corner cases when this error is thrown and " +
                            "handled by design; however, one of the most common reasons " +
                            "for this exception is re-running  BC2G on blocks that were " +
                            "already processed without clearing the database first. {error}",
                            e.Message);

                        foreach (var utxo in utxos)
                        {
                            var saved = false;
                            var tries = 0;
                            while (!saved && tries++ < 3)
                            {
                                try
                                {
                                    var existingUtxo = c.Utxos.Find(utxo.Id);
                                    if (existingUtxo == null)
                                    {
                                        await c.Utxos.AddAsync(utxo, cT);
                                    }
                                    else
                                    {
                                        existingUtxo.AddCreatedIn(utxo.CreatedIn);
                                        existingUtxo.AddReferencedIn(utxo.ReferencedIn);
                                    }

                                    await c.SaveChangesAsync(cT);
                                    saved = true;
                                    break;
                                }
                                catch (Exception _e)
                                {
                                    switch (_e)
                                    {
                                        case DbUpdateConcurrencyException:
                                        case DbUpdateException when (
                                        _e.InnerException is PostgresException _pe &&
                                        _pe.SqlState == "23505"):
                                            // Let it retry.
                                            Thread.Sleep(5000);
                                            break;
                                        default:
                                            throw;
                                    }

                                }
                            }

                            if (!saved)
                                throw;
                        }
                        break;

                    default:
                        throw;
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
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
}
