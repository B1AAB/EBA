using BC2G.CLI;
using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Model;
using BC2G.Serializers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;

namespace BC2G
{
    public class Orchestrator : IDisposable
    {
        private readonly HttpClient _client;
        private bool disposed = false;

        private readonly string _statusFilename;
        private readonly Options _options;

        public Logger Logger { set; get; }
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

                Logger = new Logger(
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
            Console.CursorVisible = false;
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

            if (_options.ToExclusive <= _options.FromInclusive)
            {
                Logger.LogWarning(
                    $"The Start block height must be smaller than the end " +
                    $"block height: `{_options.FromInclusive}` is not less " +
                    $"than `{_options.ToExclusive}`.");
                return false;
            }

            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                await TraverseBlocksAsync(agent, cancellationToken);
                stopwatch.Stop();
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.Log(
                        $"Cancelled successfully.",
                        writeLine: true,
                        color: ConsoleColor.Yellow);
                }
                else
                {
                    Logger.Log(
                        $"All process finished successfully in {stopwatch.Elapsed}.",
                        writeLine: true,
                        color: ConsoleColor.Green);
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return false;
            }
            finally
            {
                stopwatch.Stop();
            }

            Console.CursorVisible = true;
            return true;
        }

        private bool TryGetBitCoinAgent(out BitcoinAgent agent)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            agent = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            try
            {
                agent = new BitcoinAgent(_client, Logger);
                if (!agent.IsConnected)
                    throw new ClientInaccessible();
                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(
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
                    Logger.LogException(
                        "Received empty string as chain name " +
                        "from the chaininfo endpoint.");
                    return false;
                }

                if (chainInfo.Chain != "main")
                {
                    Logger.LogException(
                        $"Required to be on the `main` chain, " +
                        $"but the bitcoin client is on the " +
                        $"`{chainInfo.Chain}` chain.");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed getting chain info: {e.Message}");
                return false;
            }
        }

        private async Task TraverseBlocksAsync(
            BitcoinAgent agent, CancellationToken cancellationToken)
        {
            var blocksStatistics = new ConcurrentQueue<BlockStatistics>();

            var individualBlocksDir = Path.Combine(_options.OutputDir, "individual_blocks");
            if (!Directory.Exists(individualBlocksDir))
                Directory.CreateDirectory(individualBlocksDir);

            using var mapper = new AddressToIdMapper(
                _options.AddressIdMappingFilename, 
                cancellationToken);

            using var txCache = new TxCache(_options.OutputDir);
            using var serializer = new CSVSerializer(mapper);

            var graphsBuffer = new AutoPersistent(
                Path.Combine(_options.OutputDir, "edges.csv"),
                mapper, 
                cancellationToken);

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

            Logger.Log(
                $"Traversing blocks [{_options.FromInclusive:n0}, " +
                $"{_options.ToExclusive:n0}):", writeLine: true);
            Logger.InitBlocksTraverseLog(_options.FromInclusive, _options.ToExclusive);
            AsyncConsole.BookmarkCurrentLine();

            for (int height = _options.LastProcessedBlock + 1; height < _options.ToExclusive; height++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogCancelledTasks(
                        new BPS[]
                        {
                            BPS.GetBlockHashCancelled,
                            BPS.GetBlockCancelled,
                            BPS.ProcessTransactionsCancelled,
                            BPS.SerializeCancelled,
                            BPS.Cancelled
                        });
                    break;
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Logger.LogStartProcessingBlock(height);
                var blockStats = new BlockStatistics(height);

                Logger.LogBlockProcessStatus(BPS.GetBlockHash);
                var blockHash = await agent.GetBlockHash(height);
                Logger.LogBlockProcessStatus(BPS.GetBlockHashDone, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogCancelledTasks(
                        new BPS[]
                        {
                            BPS.GetBlockCancelled,
                            BPS.ProcessTransactionsCancelled,
                            BPS.SerializeCancelled,
                            BPS.Cancelled
                        });
                    break;
                }

                Logger.LogBlockProcessStatus(BPS.GetBlock);
                var block = await agent.GetBlock(blockHash);
                Logger.LogBlockProcessStatus(BPS.GetBlockDone, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogCancelledTasks(
                        new BPS[]
                        {
                            BPS.ProcessTransactionsCancelled,
                            BPS.SerializeCancelled,
                            BPS.Cancelled
                        });
                    break;
                }

                Logger.LogBlockProcessStatus(BPS.ProcessTransactions);
                GraphBase graph = new();
                try
                {
                    graph = await agent.GetGraph(block, txCache, cancellationToken);
                    Logger.LogBlockProcessStatus(BPS.ProcessTransactionsDone, stopwatch.Elapsed.TotalSeconds);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogCancelledTasks(
                        new BPS[]
                        {
                            BPS.ProcessTransactionsCancelled,
                            BPS.SerializeCancelled,
                            BPS.Cancelled
                        });
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogCancelledTasks(
                        new BPS[]
                        {
                            BPS.SerializeCancelled,
                            BPS.Cancelled
                        });
                    break;
                }

                //graphsBuffer.Enqueue(graph);
                graphsBuffer.Enqueue(graph);


                Logger.LogBlockProcessStatus(BPS.Serialize);
                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"), blockStats);
                _options.LastProcessedBlock = height;
                await JsonSerializer<Options>.SerializeAsync(_options, _statusFilename);
                Logger.LogBlockProcessStatus(BPS.SerializeDone, stopwatch.Elapsed.TotalSeconds);

                stopwatch.Stop();
                blockStats.Runtime = stopwatch.Elapsed;
                blocksStatistics.Enqueue(blockStats);

                Logger.LogFinishProcessingBlock(blockStats.Runtime.TotalSeconds);
            }

            Logger.LogFinishTraverse(cancellationToken.IsCancellationRequested);
            //var graphsBufferFilename = Path.Combine(_options.OutputDir, "edges.csv");
            //Logger.Log($"Serializing all edges in `{graphsBufferFilename}`.", true);
            //serializer.Serialize(graphsBuffer, graphsBufferFilename);

            Logger.Log("Serializing block status", true);
            BlocksStatisticsSerializer.Serialize(
                blocksStatistics,
                Path.Combine(_options.OutputDir, "blocks_stats.tsv"));

            // At this method's exist, the dispose method of
            // the types wrapped in `using` will be called that
            // finalizes persisting output.
            Logger.Log("Finalizing serialized files.", true);
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
                    Logger.Dispose();

                disposed = true;
            }
        }
    }
}
