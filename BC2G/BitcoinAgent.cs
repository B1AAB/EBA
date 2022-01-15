using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Model;
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

        public BitcoinAgent(HttpClient client)
        {
            _client = client;
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
        }

        public async Task<GraphBase> GetGraph(Block block, TxCache txCache)
        {
            /// Why using "mediantime" and not "time"? see the following BIP:
            /// https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
            uint timestamp = block.MedianTime;

            var g = new GraphBase();

            /// By definition, each block has a generative block that is the 
            /// reward of the miner. Hence, this should never raise an 
            /// exception if the block is not corrupt.
            var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);
            var rewardAddresses = new List<string>();
            foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
            {
                output.TryGetAddress(out string address);
                address = g.AddTarget(address, output.Value);
                rewardAddresses.Add(address);
                txCache.Add(coinbaseTx.Txid, output.Index, address, output.Value);
            }

            g.UpdateGraph(timestamp);

            // Updating graph (UpdateGraph()) is not thread safe, 
            // hence, cannot process transactions in parallel.
            foreach (var tx in block.Transactions.Where(x => !x.IsCoinbase))
            {
                foreach (var input in tx.Inputs)
                {
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
                    output.TryGetAddress(out string address);
                    g.AddTarget(address, output.Value);
                    txCache.Add(tx.Txid, output.Index, address, output.Value);
                }

                g.UpdateGraph(timestamp, rewardAddresses);
            }

            return g;
        }

        private async Task<Stream> GetResource(string endpoint, string hash)
        {
            try
            {
                return await SendGet($"{endpoint}/{hash}.json");
            }
            catch (Exception e) when (e is not ClientInaccessible)
            {
                throw new Exception($"Invalid hash `{hash}`.");
            }
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
        }
    }
}
