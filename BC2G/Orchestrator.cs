using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Model;
using BC2G.Serializers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace BC2G
{
    public class Orchestrator : IDisposable
    {
        public string AddressIdFilename { get; }
        public string StatusFilename { get; }

        public string LogFile { private set; get; }

        public ConcurrentQueue<BlockStatistics> BlocksStatistics { set; get; } = new();

        private readonly string _outputDir;

        private readonly HttpClient _client;

        private Logger _logger;
        private readonly string _loggerRepository;
        private const string _defaultLoggerRepoName = "EventsLog";
        private readonly string _loggerTimeStampFormat = "yyyyMMdd_HHmmssfffffff";

        private bool disposed = false;

        public Orchestrator(
            string outputDir, HttpClient client, 
            string statusFilename = "status.json", 
            string addressIdFilename = "address_id.csv")
        {
            _outputDir = outputDir;

            _loggerRepository = 
                _defaultLoggerRepoName + "_" + 
                DateTime.Now.ToString(
                    _loggerTimeStampFormat, 
                    CultureInfo.InvariantCulture);
            LogFile = Path.Join(_outputDir, _loggerRepository + ".txt");

            AddressIdFilename = Path.Combine(_outputDir, addressIdFilename);
            StatusFilename = Path.Combine(_outputDir, statusFilename);

            _client = client;
            if (!EnsureOutputDirectory())
                throw new Exception();

            if (!TrySetupLogger())
                throw new Exception();
        }

        public async Task<bool> RunAsync(
            CancellationToken cancellationToken,
            int? from = null, int? to = null)
        {
            if (!TryLoadStatus(out var status))
                return false;

            if (!TryGetBitCoinAgent(out var agent))
                return false;

            if (!AssertChain(agent, out ChainInfo chaininfo))
                return false;

            if (cancellationToken.IsCancellationRequested)
                return false;

            from ??= status.LastProcessedBlock + 1;
            to ??= chaininfo.Blocks;
            if (to > from)
            {
                try
                {
                    status.StartBlock = (int)from;
                    status.EndBlock = (int)to;
                    await TraverseBlocksAsync(agent, status, cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogException(e);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning(
                    $"The Start block height must be smaller than the end " +
                    $"block height: `{from}` is not less than `{to}`.");
                return false;
            }

            return true;
        }

        private bool EnsureOutputDirectory()
        {
            try
            {
                Directory.CreateDirectory(_outputDir);

                var tmpFile = Path.Combine(_outputDir, "tmp_access_test");
                File.Create(tmpFile).Dispose();
                File.Delete(tmpFile);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogExceptionStatic(
                    $"Require write access to the path {_outputDir}: " +
                    $"{ex.Message}");

                return false;
            }
        }

        private bool TrySetupLogger()
        {
            try
            {
                _logger = new Logger(
                    LogFile, _loggerRepository, Guid.NewGuid().ToString(),
                    _outputDir, "2GB");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogExceptionStatic($"Logger setup failed: {e.Message}");
                return false;
            }
        }

        private bool TryLoadStatus(out Status status)
        {
            status = new();

            try
            {   
                status = JsonSerializer<Status>.DeserializeAsync(StatusFilename).Result;
                return true;
            }
            catch (Exception e)
            {
                Logger.LogExceptionStatic(
                    $"Failed loading status from `{StatusFilename}`: {e.Message}");
                return false;
            }
        }

        private bool TryGetBitCoinAgent(out BitcoinAgent agent)
        {
            // Cannot convert null literal to non-nullable reference type.
            #pragma warning disable CS8625
            agent = null;
            #pragma warning restore CS8625

            try
            {
                agent = new BitcoinAgent(_client);
                if (!agent.IsConnected)
                    throw new ClientInaccessible();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogException(
                    $"Failed to create/access BitcoinAgent: " +
                    $"{e.Message}");
                return false;
            }
        }

        private bool AssertChain(BitcoinAgent agent, out ChainInfo chainInfo)
        {
            chainInfo = new ChainInfo();

            try
            {
                chainInfo = agent.GetChainInfoAsync().Result;
                if (string.IsNullOrEmpty(chainInfo.Chain))
                {
                    _logger.LogException(
                        "Received empty string as chain name " +
                        "from the chaininfo endpoint.");
                    return false;
                }

                if (chainInfo.Chain != "main")
                {
                    _logger.LogException(
                        $"Required to be on the `main` chain, " +
                        $"but the bitcoin client is on the " +
                        $"`{chainInfo.Chain}` chain.");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogException($"Failed getting chain info: {e.Message}");
                return false;
            }
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

            /*
            var myList = new List<string>(new string[10000]);
            Parallel.ForEach(myList, item => {
                _logger.LogTraverse(
                    Thread.CurrentThread.ManagedThreadId, 
                    new Random().Next(0, 1000).ToString());
                Thread.Sleep(1000);
                _logger.LogTraverse(Thread.CurrentThread.ManagedThreadId, "end");
            });*/

            for (int height = status.StartBlock; height < status.EndBlock; height++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _logger.Log($"\rProcessing block {height}", newLine: false);
                var blockStats = new BlockStatistics(height);
                var blockHash = await agent.GetBlockHash(height);

                if (cancellationToken.IsCancellationRequested)
                    break;
                var block = await agent.GetBlock(blockHash);

                if (cancellationToken.IsCancellationRequested)
                    break;
                var graph = await agent.GetGraph(block, txCache, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                graphsBuffer.Enqueue(graph);

                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"), blockStats);
                status.LastProcessedBlock = height;
                await JsonSerializer<Status>.SerializeAsync(status, StatusFilename);

                stopwatch.Stop();
                blockStats.Runtime = stopwatch.Elapsed;
                BlocksStatistics.Enqueue(blockStats);
                _logger.LogTraverse(0, "", blockStats.Runtime.TotalSeconds);

                //Console.WriteLine($"Block {height} processed.");
            }

            serializer.Serialize(graphsBuffer, Path.Combine(_outputDir, "edges.csv"));

            BlocksStatisticsSerializer.Serialize(
                BlocksStatistics,
                Path.Combine(_outputDir, "blocks_stats.tsv"));
        }

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    _logger.Dispose();

                disposed = true;
            }
        }
    }
}
