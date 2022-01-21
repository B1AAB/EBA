using BC2G.CLI;
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
        private readonly HttpClient _client;
        private bool disposed = false;

        private readonly string _statusFilename;
        private readonly Options _options;

        private readonly Logger _logger;
        private const string _defaultLoggerRepoName = "EventsLog";
        private readonly string _loggerTimeStampFormat = "yyyyMMdd_HHmmssfffffff";
        private readonly string _maxLogfileSize = "2GB";

        public Orchestrator(
            Options options, 
            HttpClient client, 
            string statusFilename)
        {
            _client = client;
            _options = options;
            _statusFilename = statusFilename;

            // Create the output directory if it does not exist,
            // and assert if can write to the given directory.
            try
            {
                Directory.CreateDirectory(_options.OutputDir);

                var tmpFile = Path.Combine(_options.OutputDir, "tmp_access_test");
                File.Create(tmpFile).Dispose();
                File.Delete(tmpFile);
            }
            catch (Exception e)
            {
                Logger.LogExceptionStatic(
                    $"Require write access to the path {_options.OutputDir}: " +
                    $"{e.Message}");
                throw;
            }

            // Set up logger. 
            try
            {
                var _loggerRepository = 
                    _defaultLoggerRepoName + "_" + 
                    DateTime.Now.ToString(
                        _loggerTimeStampFormat, 
                        CultureInfo.InvariantCulture);

                _logger = new Logger(
                    Path.Join(_options.OutputDir, _loggerRepository + ".txt"),
                    _loggerRepository,
                    Guid.NewGuid().ToString(),
                    _options.OutputDir, 
                    _maxLogfileSize);
            }
            catch (Exception e)
            {
                Logger.LogExceptionStatic($"Logger setup failed: {e.Message}");
                throw;
            }
        }

        public async Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            if (!TryGetBitCoinAgent(out var agent))
                return false;

            if (!AssertChain(agent, out ChainInfo chaininfo))
                return false;

            if (cancellationToken.IsCancellationRequested)
                return false;

            if (_options.FromInclusive == -1)
                _options.FromInclusive = _options.LastProcessedBlock + 1;
            if (_options.ToExclusive == -1)
                _options.ToExclusive = chaininfo.Blocks;

            if (_options.ToExclusive > _options.FromInclusive)
            {
                try
                {
                    await TraverseBlocksAsync(agent, cancellationToken);
                    _logger.Log(
                        "All process finished successfully.",
                        newLine: true,
                        color: ConsoleColor.Green);
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
                    $"block height: `{_options.FromInclusive}` is not less " +
                    $"than `{_options.ToExclusive}`.");
                return false;
            }

            return true;
        }

        private bool TryGetBitCoinAgent(out BitcoinAgent agent)
        {
            // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625
            agent = null;
#pragma warning restore CS8625

            try
            {
                agent = new BitcoinAgent(_client, _logger);
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
            BitcoinAgent agent, CancellationToken cancellationToken)
        {
            var graphsBuffer = new ConcurrentQueue<GraphBase>();
            var blocksStatistics = new ConcurrentQueue<BlockStatistics>();

            var individualBlocksDir = Path.Combine(_options.OutputDir, "individual_blocks");
            if (!Directory.Exists(individualBlocksDir))
                Directory.CreateDirectory(individualBlocksDir);

            using var mapper = new AddressToIdMapper(_options.AddressIdMappingFilename);
            using var txCache = new TxCache(_options.OutputDir);
            using var serializer = new CSVSerializer(mapper);

            // Parallelizing block traversal has more disadvantages than
            // advantages it could bring. One draw back is related to
            // caching transactions, where of a block are cached for faster
            // processing of subsequent blocks, however, when parallelized
            // this optimization may not be fully applicable. For instance,
            // consider traversing blocks 0-200 and the parallelizer
            // partitioner to split the interval to thread_1: 0-100 and
            // thread_2: 100-200. If blocks 100-200 are referencing
            // transactions in blocks 0-100, those transactions may not
            // be in cache yet, hence, BitCoinAgent will need to query
            // Bitcoin client for those transactions, which is considerably
            // slower than using the built-in cache. Partitioner can be
            // adjusted to assign threads as thread_1:0, thread_2:1,
            // thread_1:2, thread_2:3, and so on. This might suffer less
            // than the previous partitioning, and a good techniuqe to
            // implement it is TPL. However, it needs to be tested
            // if/how-much performance optimization it delivers and if
            // it balaces with complications of implementing it.

            _logger.Log(
                $"Traversing blocks [{_options.FromInclusive:n0}, " +
                $"{_options.ToExclusive:n0}):", newLine: true);
            _logger.CursorTop = Console.CursorTop;

            for (int height = _options.FromInclusive; height < _options.ToExclusive; height++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _logger.LogStartProcessingBlock(height);
                var blockStats = new BlockStatistics(height);

                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlockHash);
                var blockHash = await agent.GetBlockHash(height);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlockHash, false, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlock);
                var block = await agent.GetBlock(blockHash);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlock, false, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogStatusProcessingBlock(BlockProcessStatus.ProcessTransactions);
                var graph = await agent.GetGraph(block, txCache, cancellationToken);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.ProcessTransactions, false, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                    break;

                graphsBuffer.Enqueue(graph);

                _logger.LogStatusProcessingBlock(BlockProcessStatus.Serialize);
                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"), blockStats);
                _options.LastProcessedBlock = height;
                await JsonSerializer<Options>.SerializeAsync(_options, _statusFilename);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.Serialize, false, stopwatch.Elapsed.TotalSeconds);

                stopwatch.Stop();
                blockStats.Runtime = stopwatch.Elapsed;
                blocksStatistics.Enqueue(blockStats);

                _logger.LogFinishProcessingBlock(height, blockStats.Runtime.TotalSeconds);
            }

            var graphsBufferFilename = Path.Combine(_options.OutputDir, "edges.csv");
            _logger.Log($"Serializing all edges in `{graphsBufferFilename}`.", newLine: true);
            serializer.Serialize(graphsBuffer, graphsBufferFilename);

            _logger.Log("Serializing block status", newLine: true);
            BlocksStatisticsSerializer.Serialize(
                blocksStatistics,
                Path.Combine(_options.OutputDir, "blocks_stats.tsv"));

            // At this method's exist, the dispose method of
            // the types wrapped in `using` will be called that
            // finalizes persisting output.
            _logger.Log("Finalizing serialized files.", newLine: true);
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
