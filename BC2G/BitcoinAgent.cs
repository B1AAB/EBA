using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Model;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace BC2G
{
    public class BitcoinAgent
    {
        /// <summary>
        /// Sets and gets the REST API endpoint of the Bitcoin client.
        /// </summary>
        public Uri BaseUri { set; get; } = new Uri("http://127.0.0.1:8332/rest/");

        private readonly HttpClient _client;

        private Logger _logger;

        public BitcoinAgent(HttpClient client, Logger logger)
        {
            _client = client;
            _logger = logger;
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
                throw new Exception($"Invalid height {height}");
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
            /*
            Stream? stream = null;
            try
            {
                stream = await GetResource("tx", hash);
            }
            catch (Exception e)
            {

            }

            return
                JsonSerializer.Deserialize<Transaction>(stream)
                ?? throw new Exception("Invalid transaction.");*/
        }



        public async Task<GraphBase> GetGraph(
    Block block, TxCache txCache,
    CancellationToken cancellationToken)
        {
            /// Why using "mediantime" and not "time"? see the following BIP:
            /// https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
            uint timestamp = block.MedianTime;

            var g = new GraphBase();

            var txGraph = new TransactionGraph();

            /// By definition, each block has a generative block that is the
            /// reward of the miner. Hence, this should never raise an 
            /// exception if the block is not corrupt.
            var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);
            var rewardAddresses = new List<string>();
            foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
            {
                output.TryGetAddress(out string address);
                //address = g.AddTarget(address, output.Value);
                address = txGraph.AddTarget(address, output.Value);
                rewardAddresses.Add(address);
                txCache.Add(coinbaseTx.Txid, output.Index, address, output.Value);
            }

            g.RewardsAddresses = rewardAddresses;
            g.Merge(txGraph);
            //g.UpdateGraph(timestamp);


            await Parallel.ForEachAsync(
                block.Transactions.Where(x => !x.IsCoinbase),
                async (tx, state) =>
                {
                    var threadID = Environment.CurrentManagedThreadId;
                    _logger.LogTraverse(threadID, $"Thread {threadID}\tstarted", BlockTraverseState.Started);
                    await RunParallel(tx, g, txCache, cancellationToken);
                    _logger.LogTraverse(threadID, $"Thread {threadID}\tfinished", BlockTraverseState.Succeeded);
                });

            return g;
        }

        private async Task RunParallel(
            Transaction tx,
            GraphBase g,
            TxCache txCache,
            CancellationToken cancellationToken)
        {
            var txGraph2 = new TransactionGraph();

            if (cancellationToken.IsCancellationRequested)
                //break;
                return;

            foreach (var input in tx.Inputs)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (input.TxId != null)
                {
                    if (!txCache.TryGet(input.TxId, input.OutputIndex, out string address, out double value))
                    {
                        // Extended transaction: details of the transaction are retrieved from the bitcoin client.
                        var exTx = await GetTransaction(input.TxId);
                        var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                        if (vout == null)
                            // TODO: check when this can be null, or if it would ever happen.
                            throw new NotImplementedException();

                        vout.TryGetAddress(out address);
                        value = vout.Value;
                    }

                    txGraph2.AddSource(address, value);
                    //g.AddSource(address, value);
                }
                else
                {
                    // TODO: check if this is ever possible.
                    throw new NotImplementedException();
                }
            }

            foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                output.TryGetAddress(out string address);
                //g.AddTarget(address, output.Value);
                txGraph2.AddTarget(address, output.Value);
                txCache.Add(tx.Txid, output.Index, address, output.Value);
            }

            g.Merge(txGraph2);
            //g.UpdateGraph(timestamp, rewardAddresses);
        }

        public async Task<GraphBase> GetGraph_old(
            Block block, TxCache txCache,
            CancellationToken cancellationToken)
        {
            /// Why using "mediantime" and not "time"? see the following BIP:
            /// https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
            uint timestamp = block.MedianTime;

            var g = new GraphBase();

            var g2 = new GraphBase();
            //g2.Timestamp = timestamp;


            var txGraph = new TransactionGraph();

            /// By definition, each block has a generative block that is the
            /// reward of the miner. Hence, this should never raise an
            /// exception if the block is not corrupt.
            var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);
            var rewardAddresses = new List<string>();
            foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
            {
                output.TryGetAddress(out string address);
                address = g.AddTarget(address, output.Value);
                //address = txGraph.AddTarget(address, output.Value);
                rewardAddresses.Add(address);
                txCache.Add(coinbaseTx.Txid, output.Index, address, output.Value);
            }

            g.UpdateGraph(timestamp);

            //g2.RewardsAddresses = rewardAddresses;
            //g2.Merge(txGraph);


            /*
            var transactions = block.Transactions.Where(x => !x.IsCoinbase).ToList();
            await Parallel.ForEachAsync(
                transactions,
                //parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = 8 },
                async (tx, state) =>
                {
                    var txGraph = new TransactionGraph();
                    await RunParallel(tx, txGraph, txCache, timestamp, cancellationToken, rewardAddresses);
                    g2.Merge(txGraph);
                });*/

            //return g;
            //return g2;









            // Updating graph (UpdateGraph()) is not thread safe,
            // hence, cannot process transactions in parallel.
            foreach (var tx in block.Transactions.Where(x => !x.IsCoinbase))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                foreach (var input in tx.Inputs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (input.TxId != null)
                    {
                        if (!txCache.TryGet(input.TxId, input.OutputIndex, out string address, out double value))
                        {
                            // Extended transaction: details of the transaction are retrieved from the bitcoin client.
                            var exTx = await GetTransaction(input.TxId);
                            var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                            if (vout == null)
                                // TODO: check when this can be null, or if it would ever happen.
                                throw new NotImplementedException();

                            vout.TryGetAddress(out address);
                            value = vout.Value;
                        }

                        g.AddSource(address, value);
                    }
                    else
                    {
                        // TODO: check if this is ever possible.
                        throw new NotImplementedException();
                    }
                }

                foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    output.TryGetAddress(out string address);
                    g.AddTarget(address, output.Value);
                    txCache.Add(tx.Txid, output.Index, address, output.Value);
                }

                g.UpdateGraph(timestamp, rewardAddresses);
            }

            return g;
        }

        private async Task RunParallel(Transaction tx, TransactionGraph g, TxCache txCache, uint timestamp, CancellationToken cancellationToken, List<string> rewardAddresses)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            //break;

            foreach (var input in tx.Inputs)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (input.TxId != null)
                {
                    if (!txCache.TryGet(input.TxId, input.OutputIndex, out string address, out double value))
                    {
                        // Extended transaction: details of the transaction are retrieved from the bitcoin client.
                        var exTx = await GetTransaction(input.TxId);
                        var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                        if (vout == null)
                            // TODO: check when this can be null, or if it would ever happen.
                            throw new NotImplementedException();

                        vout.TryGetAddress(out address);
                        value = vout.Value;
                    }

                    g.AddSource(address, value);
                }
                else
                {
                    // TODO: check if this is ever possible.
                    throw new NotImplementedException();
                }
            }

            foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                output.TryGetAddress(out string address);
                g.AddTarget(address, output.Value);
                txCache.Add(tx.Txid, output.Index, address, output.Value);
            }

            //g.UpdateGraph(timestamp, rewardAddresses);
        }

        private async Task<Stream> GetResource(string endpoint, string hash)
        {
            return await SendGet($"{endpoint}/{hash}.json");
        }

        private async Task<Stream> SendGet(string endpoint)
        {
            try
            {
                //while (activeRequests >= maxConcurrentRequests)
                //    Thread.Sleep(1000);

                //Interlocked.Increment(ref activeRequests);
                return await _client.GetStreamAsync(
                    new Uri(BaseUri, endpoint));
                //Interlocked.Decrement(ref activeRequests);

                //return response;
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

        private int maxConcurrentRequests = 5;
        private int activeRequests = 0;
    }
}
