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

        public Logger Logger { set; get; }
        private const string _defaultLoggerRepoName = "events_log";
        private readonly string _loggerTimeStampFormat = "yyyyMMdd_HHmmssfffffff";
        private readonly string _maxLogfileSize = "2GB";

        public Orchestrator(Options options, HttpClient client, string statusFilename)
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

        public async Task<bool> RunAsync(CancellationToken cT)
        {
            // TODO: these two may better to move the constructor. 
            if (!TryGetBitCoinAgent(cT, out var agent, out var txCache))
                return false;

            if (!AssertChain(agent, out ChainInfo chaininfo))
                return false;

            if (cT.IsCancellationRequested)
                return false;

            if (_options.FromInclusive == -1)
                _options.FromInclusive = _options.LastProcessedBlock + 1;
            if (_options.ToExclusive == -1)
                _options.ToExclusive = chaininfo.Blocks;

            if (_options.ToExclusive <= _options.FromInclusive)
            {
                Logger.LogException(
                    $"The Start block height must be smaller than the end " +
                    $"block height: `{_options.FromInclusive}` is not less " +
                    $"than `{_options.ToExclusive}`.");
                return false;
            }

            if (_options.FromInclusive < 0)
            {
                Logger.LogException($"Invalid Start block height {_options.FromInclusive}");
                return false;
            }

            if(_options.ToExclusive < 0)
            {
                Logger.LogException($"Invalid To block height {_options.ToExclusive}");
                return false;
            }

            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                await TraverseBlocksAsync(agent, cT);

                while (true)
                {
                    if (txCache.CanClose)
                        break;
                    Thread.Sleep(500);
                }

                stopwatch.Stop();
                if (cT.IsCancellationRequested)
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
                agent.Dispose();
                txCache.Dispose();
            }

            return true;
        }

        private bool TryGetBitCoinAgent(CancellationToken cT, out BitcoinAgent agent, out TxCache txCache)
        {
            try
            {
                txCache = new TxCache(_options.OutputDir, cT);
                agent = new BitcoinAgent(_client, txCache, Logger, cT);

                if (!agent.IsConnected)
                    throw new ClientInaccessible();
                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(
                    $"Failed to create/access BitcoinAgent: " +
                    $"{e.Message}");

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                agent = null;
                txCache = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
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
            BitcoinAgent agent, CancellationToken cT)
        {
            var individualBlocksDir = Path.Combine(_options.OutputDir, "individual_blocks");
            if (_options.CreatePerBlockFiles && !Directory.Exists(individualBlocksDir))
                Directory.CreateDirectory(individualBlocksDir);

            /* TODO: This object does not scale, 
             * its memory requirement grows linearly w.r.t. to 
             * the blocks traversed. This should not be needed
             * at all when moved to db, but meanwhile, is there
             * a better solution for running on machines with 
             * less than 16GB of RAM?! 
             */
            using var mapper = new AddressToIdMapper(
                _options.AddressIdMappingFilename,
                AddressToIdMapper.Deserialize(_options.AddressIdMappingFilename),
                cT);
            agent.AddressToIdMapper = mapper;

            using var serializer = new CSVSerializer(mapper);

            using var pGraphStat = new PersistentGraphStatistics(
                Path.Combine(_options.OutputDir, "blocks_stats.tsv"),
                cT);

            using var gBuffer = new PersistentGraphBuffer(
                Path.Combine(_options.OutputDir, "nodes.tsv"),
                Path.Combine(_options.OutputDir, "edges.tsv"),                
                mapper,
                pGraphStat,
                Logger,
                cT);

            Logger.Log(
                $"Traversing blocks [{_options.FromInclusive:n0}, " +
                $"{_options.ToExclusive:n0}):");

            var blockHeightQueue = new ConcurrentQueue<int>();
            for (int h = _options.LastProcessedBlock + 1;
                     h < _options.ToExclusive;
                     h += _options.Granularity)
                blockHeightQueue.Enqueue(h);

            Logger.InitBlocksTraverse(_options.FromInclusive, _options.ToExclusive, blockHeightQueue.Count);

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _options.MaxConcurrentBlocks
            };

            // Persist the start point so to at least have the starting point
            // in case the program fails without a chance to persist the current status.
            await JsonSerializer<Options>.SerializeAsync(_options, _statusFilename);

            // Have tested TPL dataflow as alternative to Parallel.For,
            // it adds more complexity with little performance improvements,
            // and in some cases, slower than Parallel.For and sequential traversal.
            Parallel.For(0, blockHeightQueue.Count, parallelOptions, (i, state) =>
            {
                if (cT.IsCancellationRequested)
                    state.Stop();

                blockHeightQueue.TryDequeue(out var h);
                ProcessBlock(agent, gBuffer, serializer, h, individualBlocksDir, cT).Wait();

                if (cT.IsCancellationRequested)
                    state.Stop();
            });

            //Logger.LogFinishTraverse(cancellationToken.IsCancellationRequested);

            // At this method's exist, the dispose method of
            // the types wrapped in `using` will be called that
            // finalizes persisting output.
            Logger.Log("Finalizing serialized files.");
            await JsonSerializer<Options>.SerializeAsync(_options, _statusFilename);

            // TODO: this is not a good strategy, it has two drawbacks: 
            // - it is an infinite loop with the assumption that the
            // condition will be met eventually, but there is a chance
            // that the condition is not met in a given time, so it
            // should break with a timeout;
            // - the sleep blocks other threads, so a background task 
            // will never end.
            while(true)
            {
                if (mapper.CanDispose && gBuffer.CanDispose)
                    break;
                Thread.Sleep(500);
            }
        }

        /* TODO: The following error may occur, 
         * (a) why it occurs? 
         * (b) when it occurs not every persistence 
         * object is updated, in particular, the status is updated. 
         */

        private async Task ProcessBlock(
            BitcoinAgent agent,
            PersistentGraphBuffer gBuffer,
            CSVSerializer serializer,
            int height,
            string individualBlocksDir,
            CancellationToken cT)
        {
            if (cT.IsCancellationRequested) return;

            Logger.LogStartProcessingBlock(height);

            BlockGraph graph;
            try { graph = await agent.GetGraph(height); }
            catch (OperationCanceledException) { return; }
            catch { throw; }

            gBuffer.Enqueue(graph);

            if (_options.CreatePerBlockFiles)
                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"));

            _options.LastProcessedBlock = height;
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
                {
                    Logger.Dispose();
                    //_txCache.Dispose();
                }

                disposed = true;
            }
        }
    }
}
