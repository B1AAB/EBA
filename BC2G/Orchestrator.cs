using BC2G.Exceptions;
using BC2G.Model;
using BC2G.Serializers;

namespace BC2G
{
    public class Orchestrator
    {
        /// <summary>
        /// Should be Fully quantifying path
        /// </summary>
        public string AddressIdFilename { set; get; } = "address_id.csv";
        /// <summary>
        /// Should be fully quantifying path
        /// </summary>
        public string StatusFilename { set; get; } = "status.json";

        private readonly string _outputDir;

        private readonly HttpClient _client;

        public Orchestrator(string outputDir, HttpClient client)
        {
            _outputDir = outputDir;
            Directory.CreateDirectory(_outputDir);

            AddressIdFilename = Path.Combine(
                _outputDir, AddressIdFilename);
            StatusFilename = Path.Combine(
                _outputDir, StatusFilename);

            _client = client;
        }

        public async Task RunAsync(int? from = null, int? to = null)
        {
            CanWriteToOutputDir();
            var status = await LoadStatus();
            var agent = new BitcoinAgent(_client);

            if (!agent.IsConnected)
                throw new ClientInaccessible();

            var chaininfo = await AssertChain(agent);

            from ??= status.LastBlockHeight + 1;
            to ??= chaininfo.Blocks;
            if (to > from)
                await TraverseBlocks(
                    agent,
                    status,
                    (int)from,
                    (int)to);
        }

        private void CanWriteToOutputDir()
        {
            try
            {
                var tmpFile = Path.Combine(_outputDir, "tmp_access_test");
                File.Create(tmpFile).Dispose();
                File.Delete(tmpFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Require write access to the path {_outputDir}: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private async Task<Status> LoadStatus()
        {
            Status status = new();
            try
            {
                status = await JsonSerializer<Status>.DeserializeAsync(StatusFilename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            return status;
        }

        private async Task<ChainInfo> AssertChain(BitcoinAgent agent)
        {
            var chaininfo = await agent.GetChainInfoAsync();
            if (string.IsNullOrEmpty(chaininfo.Chain))
                throw new Exception(
                    "Received empty string as chain name " +
                    "from the chaininfo endpoint.");
            else if (chaininfo.Chain != "main")
                throw new Exception(
                    $"Required to be on the `main` chain, " +
                    $"the bitcoin client is on the " +
                    $"`{chaininfo.Chain}` chain.");
            return chaininfo;
        }

        private async Task TraverseBlocks(BitcoinAgent agent, Status status, int from, int to)
        {
            using var mapper = new AddressToIdMapper(AddressIdFilename);
            for (int height = from; height < to; height++)
            {
                var blockHash = await agent.GetBlockHash(height);
                var block = await agent.GetBlock(blockHash);
                var graph = await agent.GetGraph(block);
                // the serializers is embeded in a `using` statement,
                // in order to ensure `Dispose` is called.
                using (var serializer = new CSVSerializer(mapper))
                    serializer.Serialize(graph, Path.Combine(_outputDir, $"{height}"));
                status.LastBlockHeight += 1;
                await JsonSerializer<Status>.SerializeAsync(status, StatusFilename);
            }
        }
    }
}
