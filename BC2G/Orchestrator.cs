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
        private const string _defaultLoggerRepoName = "EventsLog";
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
                await TraverseBlocksAsync(agent, cT);

                while (true)
                {
                    if (txCache.IsBufferEmpty)
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
            }

            return true;
        }

        private bool TryGetBitCoinAgent(CancellationToken cT, out BitcoinAgent agent, out TxCache txCache)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            agent = null;
            txCache = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

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

            using var mapper = new AddressToIdMapper(
                _options.AddressIdMappingFilename,
                AddressToIdMapper.Deserialize(_options.AddressIdMappingFilename),
                cT);

            using var serializer = new CSVSerializer(mapper);

            var pBlockStat = new PersistentBlockStatistics(
                Path.Combine(_options.OutputDir, "blocks_stats.tsv"),
                cT);

            var gBuffer = new PersistentGraphBuffer(
                Path.Combine(_options.OutputDir, "edges.csv"),
                mapper,
                pBlockStat,
                Logger,
                cT);

            Logger.Log(
                $"Traversing blocks [{_options.FromInclusive:n0}, " +
                $"{_options.ToExclusive:n0}):", writeLine: true);

            // test:
            _options.FromInclusive = 700000;
            _options.LastProcessedBlock = _options.FromInclusive;
            _options.ToExclusive = 700005;

            Logger.InitBlocksTraverse(_options.FromInclusive, _options.ToExclusive);

            var blockHeightQueue = new ConcurrentQueue<int>();
            for (int h = _options.LastProcessedBlock + 1; h < _options.ToExclusive; h++)
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
            Logger.Log("Finalizing serialized files.", true);
            await JsonSerializer<Options>.SerializeAsync(_options, _statusFilename);

            while(true)
            {
                if (mapper.IsBufferEmpty && gBuffer.IsBufferEmpty)
                    break;
                Thread.Sleep(500);
            }
        }

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
