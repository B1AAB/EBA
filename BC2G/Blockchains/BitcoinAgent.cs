using BC2G.Graph;
using BC2G.Infrastructure;
using BC2G.Infrastructure.StartupSolutions;
using BC2G.Model;
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

            
            cT.ThrowIfCancellationRequested();
            await DatabaseContext.OptimisticAddOrUpdate(dbContext, _dbContextFactory, cT);
            dbContext.Dispose();
            cT.ThrowIfCancellationRequested();
            await DatabaseContext.OptimisticAddOrUpdate(utxos.Values, _dbContextFactory, cT);
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

                // Do NOT pass the cancelation token to following FindAsync, it seems there are 
                // known complications related to this: https://github.com/dotnet/efcore/issues/12012
                // #pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
                // var utxo = await context.Utxos.FindAsync(Utxo.GetId(input.TxId, input.OutputIndex));
                // #pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods

                Utxo? utxo;
                var id = Utxo.GetId(input.TxId, input.OutputIndex);
                lock (dbContextLock)
                    utxo = dbContext.Utxos.Find(id);

                if (utxo == null)
                    utxos.TryGetValue(id, out utxo);

                if (utxo != null)
                {
                    value = utxo.Value;
                    address = utxo.Address;
                    utxo.AddReferencedIn(g.Block.Hash);
                    /*utxos.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                    {
                        oldValue.AddReferencedIn(refdIn);
                        return oldValue;
                    });*/
                }
                else
                {
                    // Extended transaction: details of the transaction are
                    // retrieved from the bitcoin client.
                    var exTx = await GetTransactionAsync(input.TxId, cT);
                    var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                    if (vout == null)
                        throw new NotImplementedException($"{vout} not in {input.TxId}; not expected.");

                    vout.TryGetAddress(out address);
                    value = vout.Value;

                    AddOrUpdateUtxo(
                        dbContext, dbContextLock, utxos,
                        id, address, value, exTx.BlockHash, g.Block.Hash);
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

                AddOrUpdateUtxo(
                    dbContext, dbContextLock, utxos,
                    Utxo.GetId(tx.Txid, output.Index),
                    address, output.Value, g.Block.Hash, string.Empty);
            }

            g.Stats.AddInputTxCount(tx.Inputs.Count);
            g.Stats.AddOutputTxCount(tx.Outputs.Count);
            g.Enqueue(txGraph);
        }

        private static void AddOrUpdateUtxo(
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
                    trackedUtxo.AddReferencedIn(referencedIn);
                }
                else
                {
                    var utxo = new Utxo(id, address, value, createdIn, referencedIn);

                    utxos.AddOrUpdate(utxo.Id, utxo, (_, oldValue) =>
                    {
                        oldValue.AddCreatedIn(createdIn);
                        oldValue.AddReferencedIn(referencedIn);
                        return oldValue;
                    });
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
