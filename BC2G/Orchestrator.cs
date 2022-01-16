using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Model;
using BC2G.Serializers;
using System.Collections.Concurrent;
using System.Diagnostics;

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

        public ConcurrentQueue<BlockStatistics> BlocksStatistics { set; get; } = new();

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

        public async Task RunAsync(
            CancellationToken cancellationToken,
            int? from = null, int? to = null)
        {
            CanWriteToOutputDir();
            var status = await LoadStatus();
            var agent = new BitcoinAgent(_client);

            if (!agent.IsConnected)
                throw new ClientInaccessible();

            var chaininfo = await AssertChain(agent);

            if (cancellationToken.IsCancellationRequested)
                return;

            from ??= status.LastProcessedBlock + 1;
            to ??= chaininfo.Blocks;
            if (to > from)
            {
                status.StartBlock = (int)from;
                status.EndBlock = (int)to;
                await TraverseBlocksAsync(
                    agent, status, cancellationToken);
            }
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

        private static async Task<ChainInfo> AssertChain(BitcoinAgent agent)
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

        private async Task TraverseBlocksAsync(
            BitcoinAgent agent, Status status,
            CancellationToken cancellationToken)
        {
            var graphsBuffer = new ConcurrentQueue<GraphBase>();

            var individualBlocksDir = Path.Combine(_outputDir, "individual_blocks");
            if (!Directory.Exists(individualBlocksDir))
                Directory.CreateDirectory(individualBlocksDir);

            using var mapper = new AddressToIdMapper(AddressIdFilename);
            using var txCache = new TxCache(_outputDir);
            using var serializer = new CSVSerializer(mapper);
            for (int height = status.StartBlock; height < status.EndBlock; height++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Console.WriteLine($"Processing block {height}");
                var blockStats = new BlockStatistics(height);
                var blockHash = await agent.GetBlockHash(height);

                if (cancellationToken.IsCancellationRequested)
                    break;
                var block = await agent.GetBlock(blockHash);

                if (cancellationToken.IsCancellationRequested)
                    break;
                var graph = await agent.GetGraph(block, txCache);

                if (cancellationToken.IsCancellationRequested)
                    break;

                graphsBuffer.Enqueue(graph);

                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"), blockStats);
                status.LastProcessedBlock = height;
                await JsonSerializer<Status>.SerializeAsync(status, StatusFilename);

                stopwatch.Stop();
                blockStats.Runtime = stopwatch.Elapsed;
                BlocksStatistics.Enqueue(blockStats);

                Console.WriteLine($"Block {height} processed.");
            }

            serializer.Serialize(graphsBuffer, Path.Combine(_outputDir, "edges.csv"));

            BlocksStatisticsSerializer.Serialize(
                BlocksStatistics,
                Path.Combine(_outputDir, "blocks_stats.tsv"));
        }
    }
}
