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

        private const string _delimiter = ",";

        public Logger Logger { set; get; }
        private const string _defaultLoggerRepoName = "EventsLog";
        private readonly string _loggerTimeStampFormat = "yyyyMMdd_HHmmssfffffff";
        private readonly string _maxLogfileSize = "2GB";

        private BitcoinAgent _agent;

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

        public async Task<bool> RunAsync(CancellationToken ct)
        {
            // TODO: these two may better to move the constructor. 
            Console.CursorVisible = false;
            if (!TryGetBitCoinAgent(out var agent))
                return false;
            _agent = agent;

            if (!AssertChain(agent, out ChainInfo chaininfo))
                return false;

            if (ct.IsCancellationRequested)
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
                await TraverseBlocksAsync(agent, ct);
                stopwatch.Stop();
                if (ct.IsCancellationRequested)
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
            var individualBlocksDir = Path.Combine(_options.OutputDir, "individual_blocks");
            if (_options.CreatePerBlockFiles && !Directory.Exists(individualBlocksDir))
                Directory.CreateDirectory(individualBlocksDir);

            using var mapper = new AddressToIdMapper(
                _options.AddressIdMappingFilename,
                AddressToIdMapper.Deserialize(_options.AddressIdMappingFilename),
                cancellationToken);

            using var txCache = new TxIndex(_options.OutputDir, cancellationToken);
            using var serializer = new CSVSerializer(mapper);

            var pBlockStat = new PersistentBlockStatistics(
                Path.Combine(_options.OutputDir, "blocks_stats.tsv"),
                cancellationToken);

            var gBuffer = new PersistentGraphBuffer(
                Path.Combine(_options.OutputDir, "edges.csv"),
                mapper,
                pBlockStat,
                cancellationToken);

            Logger.Log(
                $"Traversing blocks [{_options.FromInclusive:n0}, " +
                $"{_options.ToExclusive:n0}):", writeLine: true);
            Logger.InitBlocksTraverseLog(_options.FromInclusive, _options.ToExclusive);
            AsyncConsole.BookmarkCurrentLine();

            var blockHeightQueue = new ConcurrentQueue<int>();
            for (int h = _options.LastProcessedBlock + 1; h < 20000/*_options.ToExclusive*/; h++)
                blockHeightQueue.Enqueue(h);

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _options.MaxConcurrentBlocks
            };

            // Have tested TPL dataflow as alternative to Parallel.For,
            // it adds more complexity with little performance improvements,
            // and in some cases, slower than Parallel.For and sequential traversal.
            Parallel.For(0, blockHeightQueue.Count, parallelOptions, (i, state) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    state.Break();

                blockHeightQueue.TryDequeue(out var h);
                ProcessBlock(agent, gBuffer, serializer, txCache, h, individualBlocksDir, cancellationToken).Wait();
            });

            //Logger.LogFinishTraverse(cancellationToken.IsCancellationRequested);

            // At this method's exist, the dispose method of
            // the types wrapped in `using` will be called that
            // finalizes persisting output.
            Logger.Log("Finalizing serialized files.", true);

            await JsonSerializer<Options>.SerializeAsync(_options, _statusFilename);
        }

        private async Task ProcessBlock(
            BitcoinAgent agent, 
            PersistentGraphBuffer gBuffer, 
            CSVSerializer serializer, 
            TxIndex txCache, 
            int height, 
            string individualBlocksDir, 
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            { Logger.LogCancelling(); return; }

            Logger.LogStartProcessingBlock(height);

            BlockGraph graph;
            try
            {
                // TODO: see if I can move the TxCache and cancellation to the init of agent.
                graph = await agent.GetGraph(height, txCache, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.LogCancelling();
                return;
            }
            catch
            {
                throw;
            }

            gBuffer.Enqueue(graph);

            if (_options.CreatePerBlockFiles)
            {
                Logger.LogBlockProcessStatus(BPS.Serialize);
                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"));//, blockStats);
            }

            _options.LastProcessedBlock = height;
            //Logger.LogFinishProcessingBlock(blockStats.Runtime.TotalSeconds);
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
