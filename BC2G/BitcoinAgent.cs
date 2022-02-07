using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Model;
using System.Text.Json;

namespace BC2G
{
    public class BitcoinAgent : IDisposable
    {
        /// <summary>
        /// Sets and gets the REST API endpoint of the Bitcoin client.
        /// </summary>
        public Uri BaseUri { set; get; } = new Uri("http://127.0.0.1:8332/rest/");

        private readonly HttpClient _client;

        private readonly Logger _logger;

        private readonly TxCache _txCache;

        private readonly CancellationToken _cT;

        private bool _disposed = false;

        public BitcoinAgent(HttpClient client, TxCache txCache, Logger logger, CancellationToken ct)
        {
            _client = client;
            _txCache = txCache;
            _logger = logger;
            _cT = ct;
        }

        /// <summary>
        /// Is true if it can successfully query the `chaininfo` endpoint of 
        /// the Bitcoin client via the given value of <paramref name="BaseUri"/>;
        /// false if otherwise.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    var _ = _client.GetAsync(new Uri(BaseUri, "chaininfo.json")).Result;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<ChainInfo> GetChainInfoAsync()
        {
            try
            {
                var stream = await SendGet($"chaininfo.json");
                return
                    await JsonSerializer.DeserializeAsync<ChainInfo>(stream)
                    ?? throw new Exception("Error reading chain info.");
            }
            catch (Exception e) when (e is not ClientInaccessible)
            {
                throw new Exception($"Error getting chain info.");
            }
        }

        public async Task<string> GetBlockHash(int height)
        {
            Stream stream;
            try
            {
                stream = await SendGet($"blockhashbyheight/{height}.hex");
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd().Trim();
            }
            catch (Exception e) when (e is not ClientInaccessible)
            {
                throw;
                // The exception can happen when the given block height 
                // is invalid, or the service is not availabe (e.g., the 
                // bitcoin agent is not responding. 
                // throw new Exception($"Invalid height {height}");
            }
        }

        public async Task<Block> GetBlock(string hash)
        {
            return
                await JsonSerializer.DeserializeAsync<Block>(
                    await GetResource("block", hash))
                ?? throw new Exception("Invalid block.");
        }

        public async Task<Transaction> GetTransaction(string hash)
        {
            return
                await JsonSerializer.DeserializeAsync<Transaction>(
                    await GetResource("tx", hash))
                ?? throw new Exception("Invalid transaction.");
        }

        public async Task<BlockGraph> GetGraph(int height)
        {
            if (_cT.IsCancellationRequested) throw new OperationCanceledException();

            var graph = new BlockGraph(height);

            _logger.Log($"Getting block hash; height {height}.");
            var blockHash = await GetBlockHash(height);

            if (_cT.IsCancellationRequested) throw new OperationCanceledException();

            _logger.Log($"Getting block; height: {height}.");
            var block = await GetBlock(blockHash);

            // See the following BIP on using `mediantime` instead of `time`.
            // https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
            graph.Timestamp = block.MedianTime;

            if (_cT.IsCancellationRequested) throw new OperationCanceledException();

            _logger.Log($"Getting graph; height: {height}.");
            await ProcessTxes(graph, block);

            return graph;
        }

        private async Task ProcessTxes(BlockGraph g, Block block)
        {
            var generationTxGraph = new TransactionGraph();

            // By definition, each block has a generative block that is the
            // reward of the miner. Hence, this should never raise an 
            // exception if the block is not corrupt.
            var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);
            var rewardAddresses = new List<string>();
            foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
            {
                output.TryGetAddress(out string address);
                address = generationTxGraph.AddTarget(address, output.Value);
                rewardAddresses.Add(address);
                _txCache.Add(coinbaseTx.Txid, output.Index, address, output.Value);
            }

            g.RewardsAddresses = rewardAddresses;
            g.Enqueue(generationTxGraph);

            // If cancelled, the it will throw the OperationCanceledException
            // which is caught at the orchestrator in order to better handle logging.
            var options = new ParallelOptions() { CancellationToken = _cT };
            await Parallel.ForEachAsync(
                block.Transactions.Where(x => !x.IsCoinbase),
                options,
                async (tx, _loopCancellationToken) =>
                {
                    _loopCancellationToken.ThrowIfCancellationRequested();
                    await ProcessTx(g, tx);
                });
        }

        private async Task ProcessTx(BlockGraph g, Transaction tx)
        {
            var txGraph = new TransactionGraph();
            _cT.ThrowIfCancellationRequested();

            foreach (var input in tx.Inputs)
            {
                _cT.ThrowIfCancellationRequested();

                if (!_txCache.TryGet(
                    input.TxId,
                    input.OutputIndex,
                    out string address,
                    out double value))
                {
                    // Extended transaction: details of the transaction are
                    // retrieved from the bitcoin client.
                    //var exTx = await GetTransaction(input.TxId);
                    var exTx = await GetTransaction(input.TxId);
                    var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                    if (vout == null)
                        // TODO: check when this can be null,
                        // or if it would ever happen.
                        throw new NotImplementedException();

                    vout.TryGetAddress(out address);
                    value = vout.Value;
                }

                txGraph.AddSource(address, value);
            }

            foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
            {
                _cT.ThrowIfCancellationRequested();

                output.TryGetAddress(out string address);
                txGraph.AddTarget(address, output.Value);
                _txCache.Add(tx.Txid, output.Index, address, output.Value);
            }

            g.Enqueue(txGraph);
        }

        private async Task<Stream> GetResource(string endpoint, string hash)
        {
            return await SendGet($"{endpoint}/{hash}.json");
        }

        private async Task<Stream> SendGet(string endpoint)
        {
            try
            {
                return await _client.GetStreamAsync(
                    new Uri(BaseUri, endpoint));
            }
            catch when (!IsConnected)
            {
                throw new ClientInaccessible();
            }
            catch (Exception e)
            {
                var msg = e.Message;
                if (e.InnerException != null)
                    msg += " " + e.InnerException.Message;
                throw new Exception(msg);
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
                    _txCache.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
