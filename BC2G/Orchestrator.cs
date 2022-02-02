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
using System.Text;
using System.Threading.Tasks.Dataflow;

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

        public async Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            // TODO: these two may better to move the constructor. 
            Console.CursorVisible = false;
            if (!TryGetBitCoinAgent(out var agent))
                return false;
            _agent = agent;

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

        private async Task TraverseBlocksAsync_tpl(BitcoinAgent agent, CancellationToken cancellationToken)
        {
            // Declear resource shared across every block in the pipeline.
            var edgesStream = File.AppendText(
                Path.Combine(_options.OutputDir, "edges.csv"));
            edgesStream.AutoFlush = true;

            var blockStatsStream = File.AppendText(
                Path.Combine(_options.OutputDir, "blocks_stats.tsv"));
            blockStatsStream.AutoFlush = true;

            var txCache = new TxIndex(_options.OutputDir, cancellationToken);

            var mapper = new AddressToIdMapper(
                _options.AddressIdMappingFilename,
                AddressToIdMapper.Deserialize(_options.AddressIdMappingFilename),
                cancellationToken);

            int from = _options.LastProcessedBlock + 1;
            int to = _options.ToExclusive;
            var progress = new Progress(from, to);

            // Create the pipeline.
            var blockBuffer = new BufferBlock<DataContainer>(new DataflowBlockOptions
            {
                BoundedCapacity = 100,
                CancellationToken = cancellationToken
            });

            var getBlockTB = new TransformBlock<DataContainer, DataContainer>(
                GetBlock,
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 100,
                    MaxDegreeOfParallelism = 25,//5,
                    CancellationToken = cancellationToken
                });

            var getGraphTB = new TransformBlock<DataContainer, DataContainer>(
                GetGraph,
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 100,
                    MaxDegreeOfParallelism = 25, //Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                });

            var buildGraphTB = new TransformBlock<DataContainer, DataContainer>(
                BuildGraph,
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 100,
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 8,
                    CancellationToken = cancellationToken
                });

            var serializeTB = new ActionBlock<DataContainer>(
                Serialize,
                new ExecutionDataflowBlockOptions()
                {
                    //BoundedCapacity = 100,
                    MaxDegreeOfParallelism = 1,
                    CancellationToken = cancellationToken
                });

            var linkOptions = new DataflowLinkOptions()
            { PropagateCompletion = true };

            blockBuffer.LinkTo(getBlockTB, linkOptions);
            getBlockTB.LinkTo(getGraphTB, linkOptions);
            getGraphTB.LinkTo(buildGraphTB, linkOptions);
            buildGraphTB.LinkTo(serializeTB, linkOptions);

            from = 719000;
            for (int height = from; height < to; height++)
            {
                var container = new DataContainer(
                    height, progress, edgesStream, blockStatsStream,
                    txCache, mapper, cancellationToken);

                //getBlockTB.Post(container);
                await blockBuffer.SendAsync(container);
            }

            blockBuffer.Complete();
            try
            {
                serializeTB.Completion.Wait(cancellationToken);
            }
            catch (OperationCanceledException e)
            {

            }
            catch (Exception e)
            {

            }
            finally
            {
                txCache.Dispose();
            }
        }

        private async Task<DataContainer> GetBlock(DataContainer c)
        {
            try
            {
                Logger.Log($"Start getting block info {c.BlockHeight}.");
                c.Stopwatch.Start();
                var blockHash = await _agent.GetBlockHash(c.BlockHeight);
                var block = await _agent.GetBlock(blockHash);
                c.Block = block;
            }
            catch (Exception e)
            {
                throw;
            }

            Logger.Log($"Received block hash and data for block {c.BlockHeight}.");
            return c;
        }

        private async Task<DataContainer> GetGraph(DataContainer c)
        {            
            try
            {
                Logger.Log($"Start getting block {c.BlockHeight}.");
                GraphBase graph = new(c.BlockStatistics);
                // TODO: move txCache to agent constructor. 
                graph = await _agent.GetGraph(c.Block, c.TxCache, c.BlockStatistics, c.CancellationToken);
                c.GraphBase = graph;
                Logger.Log($"Received graph for block height {c.BlockHeight}.");
                return c;
                //Logger.LogBlockProcessStatus(BPS.ProcessTransactionsDone, stopwatch.Elapsed.TotalSeconds);
            }
            catch (OperationCanceledException)
            {
                //Logger.LogCancelledTasks(
                //break;
                // TODO: FIX ME. .................................
                return c;
            }
        }

        private DataContainer BuildGraph(DataContainer c)
        {
            try
            {
                Logger.Log($"Start building graph {c.BlockHeight}.");
                c.GraphBase.MergeQueuedTxGraphs(c.CancellationToken);
                Logger.Log($"Built graph for block height {c.BlockHeight}.");
            }
            catch (Exception e)
            {

            }
            return c;
        }

        private void Serialize(DataContainer c)
        {
            try
            {
                Logger.Log($"started serialization for {c.BlockHeight}");
                var csvBuilder = new StringBuilder();
                foreach (var edge in c.GraphBase.Edges)
                    csvBuilder.AppendLine(
                        string.Join(_delimiter, new string[]
                        {
                        c.Mapper.GetId(edge.Source).ToString(),
                        c.Mapper.GetId(edge.Target).ToString(),
                        edge.Value.ToString(),
                        ((byte)edge.Type).ToString(),
                        edge.Timestamp.ToString()
                        }));
                c.EdgesStreamWriter.Write(csvBuilder.ToString());

                c.Stopwatch.Stop();
                c.BlockStatistics.Runtime = c.Stopwatch.Elapsed;
                c.BlockStatsStreamWriter.Write(c.BlockStatistics.ToString());

                //c.Progress.IncrementProcessed();
                c.Progress.RecordProcessed(c.Block.TransactionsCount, c.BlockStatistics.Runtime.TotalSeconds);
            }
            catch (Exception ex)
            {

            }
            Logger.Log(c.Progress);
        }

        private async Task TraverseBlocksAsync_parallel_for(
            BitcoinAgent agent, CancellationToken cancellationToken)
        {
            // TODO: maybe this method can be implemented better/simpler 
            // using Task Parallel Library (TPL); that can ideally replace
            // the Persistent* types, and give a more natural flow to the
            // current process.

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
            for (int h = _options.LastProcessedBlock + 1; h < _options.ToExclusive; h++)
                blockHeightQueue.Enqueue(h);


            Parallel.For(0, blockHeightQueue.Count, i =>
            {
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


        private async Task TraverseBlocksAsync(
            BitcoinAgent agent, CancellationToken cancellationToken)
        {
            // TODO: maybe this method can be implemented better/simpler 
            // using Task Parallel Library (TPL); that can ideally replace
            // the Persistent* types, and give a more natural flow to the
            // current process.

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

            /// Multiple stategies for concurrently running processing
            /// blocks are tested, among them are paralle.For,
            /// parallel.Foreach, and Tasks.Dataflow (TPL dataflow).
            /// They all add additional complexity with minor performance
            /// improvement, and sometimes (mainly with TPL), significant
            /// slow-down. 
            /// CPU profiling shows the hottest line is when sending/getting
            /// requests to/from the bitcoin clinet. Hence, the slowest 
            /// part of the application is getting data from the 
            /// Bitcoin agent. And through experiments, submitting more
            /// concurrent requests does not speed up. 

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

            for (int height = 719000 /*_options.LastProcessedBlock + 1*/; height < _options.ToExclusive; height++)
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

                //Logger.LogBlockProcessStatus(BPS.GetBlockHash);
                var blockHash = await agent.GetBlockHash(height);
                //Logger.LogBlockProcessStatus(BPS.GetBlockHashDone, stopwatch.Elapsed.TotalSeconds);

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

                //Logger.LogBlockProcessStatus(BPS.GetBlock);
                var block = await agent.GetBlock(blockHash);
                //Logger.LogBlockProcessStatus(BPS.GetBlockDone, stopwatch.Elapsed.TotalSeconds);

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

                //Logger.LogBlockProcessStatus(BPS.ProcessTransactions);
                GraphBase graph = new(blockStats);
                try
                {
                    graph = await agent.GetGraph(block, txCache, blockStats, cancellationToken);
                    //Logger.LogBlockProcessStatus(BPS.ProcessTransactionsDone, stopwatch.Elapsed.TotalSeconds);
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

                // TODO: stopwatch should not stop here, it should stop 
                // after graph and all the related data are persisted. 
                // However, since the graph and its related data are 
                // serialized using the Persistent* type, it requires
                // sending the stopwatch instance around to stop 
                // at the momemnt the process completes, which makes
                // it harder to read/follow. This should be fixed
                // when this method is implemented using TPL. 
                stopwatch.Stop();
                blockStats.Runtime = stopwatch.Elapsed;
                gBuffer.Enqueue(graph);

                if (_options.CreatePerBlockFiles)
                {
                    Logger.LogBlockProcessStatus(BPS.Serialize);
                    serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"));//, blockStats);
                }

                _options.LastProcessedBlock = height;
                //Logger.LogFinishProcessingBlock(blockStats.Runtime.TotalSeconds);
            }

            //Logger.LogFinishTraverse(cancellationToken.IsCancellationRequested);

            // At this method's exist, the dispose method of
            // the types wrapped in `using` will be called that
            // finalizes persisting output.
            Logger.Log("Finalizing serialized files.", true);

            await JsonSerializer<Options>.SerializeAsync(_options, _statusFilename);
        }

        private async Task ProcessBlock(BitcoinAgent agent, PersistentGraphBuffer gBuffer, CSVSerializer serializer, TxIndex txCache, int height, string individualBlocksDir, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Logger.LogStartProcessingBlock(height);
            var blockStats = new BlockStatistics(height);

            //Logger.LogBlockProcessStatus(BPS.GetBlockHash);
            var blockHash = await agent.GetBlockHash(height);
            //Logger.LogBlockProcessStatus(BPS.GetBlockHashDone, stopwatch.Elapsed.TotalSeconds);

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
                return;
            }

            //Logger.LogBlockProcessStatus(BPS.GetBlock);
            var block = await agent.GetBlock(blockHash);
            //Logger.LogBlockProcessStatus(BPS.GetBlockDone, stopwatch.Elapsed.TotalSeconds);

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogCancelledTasks(
                    new BPS[]
                    {
                            BPS.ProcessTransactionsCancelled,
                            BPS.SerializeCancelled,
                            BPS.Cancelled
                    });
                return;
            }

            //Logger.LogBlockProcessStatus(BPS.ProcessTransactions);
            GraphBase graph = new(blockStats);
            try
            {
                graph = await agent.GetGraph(block, txCache, blockStats, cancellationToken);
                //Logger.LogBlockProcessStatus(BPS.ProcessTransactionsDone, stopwatch.Elapsed.TotalSeconds);
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
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogCancelledTasks(
                    new BPS[]
                    {
                            BPS.SerializeCancelled,
                            BPS.Cancelled
                    });
                return;
            }

            // TODO: stopwatch should not stop here, it should stop 
            // after graph and all the related data are persisted. 
            // However, since the graph and its related data are 
            // serialized using the Persistent* type, it requires
            // sending the stopwatch instance around to stop 
            // at the momemnt the process completes, which makes
            // it harder to read/follow. This should be fixed
            // when this method is implemented using TPL. 
            stopwatch.Stop();
            blockStats.Runtime = stopwatch.Elapsed;
            gBuffer.Enqueue(graph);

            if (_options.CreatePerBlockFiles)
            {
                Logger.LogBlockProcessStatus(BPS.Serialize);
                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"));//, blockStats);
            }

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
