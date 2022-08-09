using BC2G.Blockchains;
using BC2G.CLI;
using BC2G.DAL;
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
        private GraphDB _graphDB;
        private bool disposed = false;

        private Options _options;

        public Logger Logger { set; get; }
        private const string _defaultLoggerRepoName = "events_log";
        private readonly string _loggerTimeStampFormat = "yyyyMMdd_HHmmssfffffff";
        private readonly string _maxLogfileSize = "2GB";

        private readonly CommandLineInterface _cli;
        private readonly CancellationToken _ct;

        public Orchestrator(HttpClient client, CancellationToken ct)
        {
            _ct = ct;
            _client = client;
            _cli = new CommandLineInterface(TraverseAsync, Sample);

            
            //_options = options;
            //_statusFilename = statusFilename;

            // Create the output directory if it does not exist,
            // and assert if can write to the given directory.
            /*try
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
            }*/
        }

        private void SetupLogger(Options options)
        {
            // Set up logger. 
            try
            {
                var _loggerRepository =
                    _defaultLoggerRepoName + "_" +
                    DateTime.Now.ToString(
                        _loggerTimeStampFormat,
                        CultureInfo.InvariantCulture);

                Logger = new Logger(
                    Path.Join(options.WorkingDir, _loggerRepository + ".txt"),
                    _loggerRepository,
                    Guid.NewGuid().ToString(),
                    options.WorkingDir,
                    _maxLogfileSize);
            }
            catch (Exception e)
            {
                Logger.LogExceptionStatic($"Logger setup failed: {e.Message}");
                throw;
            }
        }

        private void SetupGraphDB(Options options)
        {
            _graphDB = new GraphDB(
                options.Neo4jUri,
                options.Neo4jUser,
                options.Neo4jPassword,
                options.Neo4jImportDirectory,
                options.Neo4jCypherImportPrefix);
        }

        public async Task<int> InvokeAsync(string[] args)
        {
            return await _cli.InvokeAsync(args);
        }

        private async Task Sample(Options options)
        {
            SetupLogger(options);
            SetupGraphDB(options);

            await _graphDB.Sampling(10, 3);
        }

        private async Task<bool> TraverseAsync(Options options)
        {
            // important TODO:
            // The booleans returns of this method are ignored.
            // At the time of writing this, there are limited options
            // in the system.commandline to implement this properly. 

            _options = options;

            SetupLogger(options);
            SetupGraphDB(options);

            if (!TryGetBitCoinAgent(_ct, out var agent))
                return false;

            if (!AssertChain(agent, out ChainInfo chaininfo))
                return false;

            if (_ct.IsCancellationRequested)
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

            if (_options.ToExclusive < 0)
            {
                Logger.LogException($"Invalid To block height {_options.ToExclusive}");
                return false;
            }

            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                await TraverseBlocksAsync(agent, _ct);

                stopwatch.Stop();
                if (_ct.IsCancellationRequested)
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
            }

            return true;
        }

        private bool TryGetBitCoinAgent(CancellationToken cT, out BitcoinAgent agent)
        {
            try
            {
                agent = new BitcoinAgent(_client, Logger, cT);

                if (!agent.IsConnected)
                    throw new ClientInaccessible();
                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(
                    $"Failed to create/access BitcoinAgent: " +
                    $"{e.Message}");

                agent = default;
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
            /*
            var individualBlocksDir = Path.Combine(_options.WorkingDir, "individual_blocks");
            if (_options.CreatePerBlockFiles && !Directory.Exists(individualBlocksDir))
                Directory.CreateDirectory(individualBlocksDir);*/

            /* TODO: This object does not scale, 
             * its memory requirement grows linearly w.r.t. to 
             * the blocks traversed. This should not be needed
             * at all when moved to db, but meanwhile, is there
             * a better solution for running on machines with 
             * less than 16GB of RAM?! 
             */
            /*
            using var mapper = new AddressToIdMapper(
                _options.AddressIdMappingFilename,
                AddressToIdMapper.Deserialize(_options.AddressIdMappingFilename),
                cT);*/
            //agent.AddressToIdMapper = mapper;

            using var serializer = new CSVSerializer();//mapper);

            using var pGraphStat = new PersistentGraphStatistics(
                Path.Combine(_options.WorkingDir, "blocks_stats.tsv"),
                cT);

            using var gBuffer = new PersistentGraphBuffer(
                _graphDB,
                Path.Combine(_options.WorkingDir, "nodes.tsv"),
                Path.Combine(_options.WorkingDir, "edges.tsv"),                
                //mapper,
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
            await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile);

            // Have tested TPL dataflow as alternative to Parallel.For,
            // it adds more complexity with little performance improvements,
            // and in some cases, slower than Parallel.For and sequential traversal.
            Parallel.For(0, blockHeightQueue.Count, parallelOptions, (i, state) =>
            {
                if (cT.IsCancellationRequested)
                    state.Stop();

                blockHeightQueue.TryDequeue(out var h);
                ProcessBlock(agent, gBuffer, serializer, h, /*individualBlocksDir,*/ cT).Wait();

                if (cT.IsCancellationRequested)
                    state.Stop();
            });

            _graphDB.FinishBulkImport();

            //Logger.LogFinishTraverse(cancellationToken.IsCancellationRequested);

            // At this method's exist, the dispose method of
            // the types wrapped in `using` will be called that
            // finalizes persisting output.
            Logger.Log("Finalizing serialized files.");
            await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile);

            // TODO: this is not a good strategy, it has two drawbacks: 
            // - it is an infinite loop with the assumption that the
            // condition will be met eventually, but there is a chance
            // that the condition is not met in a given time, so it
            // should break with a timeout;
            // - the sleep blocks other threads, so a background task 
            // will never end.
            while(true)
            {
                if (/*mapper.CanDispose &&*/ gBuffer.CanDispose)
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
            //string individualBlocksDir,
            CancellationToken cT)
        {
            if (cT.IsCancellationRequested) return;

            Logger.LogStartProcessingBlock(height);

            BlockGraph graph;
            try { graph = await agent.GetGraph(height); }
            catch (OperationCanceledException) { return; }
            catch { throw; }

            gBuffer.Enqueue(graph);
            /*try
            {
                graph.MergeQueuedTxGraphs(cT);
                _graphDB.AddBlock(graph.Block).Wait(cT);
                foreach (var edge in graph.Edges)
                    _graphDB.AddEdge(graph.Block, edge).Wait(cT);

                graph.Stats.StopStopwatch();
                //_pGraphStats.Enqueue(graph.Stats.ToString());
            }
            catch (OperationCanceledException) { return; }*/

            Logger.LogFinishProcessingBlock(
                graph.Height,
                1, //_mapper.NodesCount,
                graph.EdgeCount,
                graph.Stats.Runtime.TotalSeconds);

            /*if (_options.CreatePerBlockFiles)
                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"));*/

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
