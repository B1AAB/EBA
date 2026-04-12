using EBA.Blockchains.Bitcoin.Utilities;

namespace EBA.Blockchains.Bitcoin;

public class BitcoinOrchestrator : IBlockchainOrchestrator
{
    private readonly BitcoinChainAgent _agent;
    private readonly ILogger<BitcoinOrchestrator> _logger;

    // TODO: check how this class can be improved without leveraging IHost.
    private readonly IHost _host;

    public BitcoinOrchestrator(
        BitcoinChainAgent agent,
        ILogger<BitcoinOrchestrator> logger,
        IHost host)
    {
        _agent = agent;
        _logger = logger;
        _host = host;
    }

    public async Task TraverseAsync(
        Options options,
        CancellationToken cT)
    {
        var chainInfo = await _agent.AssertChainAsync(cT);
        _logger.LogInformation("Head of the chain is at block {block:n0}.", chainInfo.Blocks);
        options.Bitcoin.Traverse.To ??= chainInfo.Blocks;

        SetupPersistedQueues(options, out var blockHeightQueue, out var failedBlocksQueue);

        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, cT);

        if (blockHeightQueue.Count == 0)
        {
            _logger.LogInformation("No blocks to process.");
            return;
        }

        cT.ThrowIfCancellationRequested();
        var stopwatch = new Stopwatch();

        try
        {
            stopwatch.Start();
            await TraverseBlocksAsync(options, blockHeightQueue, failedBlocksQueue, cT);

            stopwatch.Stop();
            _logger.LogInformation("Successfully finished traverse in {et}.", stopwatch.Elapsed);
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            if (e is TaskCanceledException || e is OperationCanceledException)
                _logger.LogInformation(
                    "Cancelled successfully. Elapsed time since the " +
                    "beginning of the process: {t}", stopwatch.Elapsed);

            throw;
        }
    }    

    public async Task DeDupAsync(
        Options options,
        CancellationToken cT)
    {
        await Deduplicator.DedupScriptNodesFile(
            options.Bitcoin.Dedup.SortedScriptNodesFilename,
            Path.Combine(options.WorkingDir, $"unique_nodes_{ScriptNode.Kind}.csv"),
            _logger,
            cT);

        await Deduplicator.ProcessTxNodesFile(
            options.Bitcoin.Dedup.SortedTxNodesFilename,
            Path.Combine(options.WorkingDir, $"unique_nodes_{TxNode.Kind}.csv"),
            _logger,
            cT);

        _logger.LogInformation("Successfully finished deduplication.");
    }

    public async Task MapMarketAsync(Options options, CancellationToken cT)
    {
        var mapper = new MarketMapper(_agent, _logger);
        await mapper.MapAsync(options, cT);
    }

    private void SetupPersistedQueues(
        Options options,
        out PersistentConcurrentQueue blocksToProcessQueue,
        out PersistentConcurrentQueue failedBlocksQueue)
    {
        var heights = new List<long>();

        if (options.Bitcoin.Traverse.BlocksListFile != null)
        {
            using var reader = new StreamReader(options.Bitcoin.Traverse.BlocksListFile);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (long.TryParse(line, out var h))
                    heights.Add(h);
                else
                    _logger.LogWarning("Ignoring malformed line: {line}", line);
            }

            _logger.LogInformation(
                "Read {n:n0} block heights from file {f}.",
                heights.Count,
                options.Bitcoin.Traverse.BlocksListFile);
        }
        else
        {
            for (int h = options.Bitcoin.Traverse.From;
                h <= options.Bitcoin.Traverse.To;
                h += options.Bitcoin.Traverse.Granularity)
                heights.Add(h);

            _logger.LogInformation(
                "Initialized the list of blocks to process using the traverse options, " +
                "with {n:n0} blocks to process in range [{from:n0}, {to:n0}] with granularity {g}.",
                heights.Count,
                options.Bitcoin.Traverse.From,
                options.Bitcoin.Traverse.To,
                options.Bitcoin.Traverse.Granularity);
        }

        var qFilename = options.Bitcoin.Traverse.BlocksToProcessListFilename;
        if (!File.Exists(qFilename))
        {
            blocksToProcessQueue = new PersistentConcurrentQueue(qFilename, heights);
            blocksToProcessQueue.Serialize();

            _logger.LogInformation(
                "File {f} not found, and initialized with {n:n0} blocks to process. ",
                qFilename,
                blocksToProcessQueue.Count);
        }
        else
        {
            blocksToProcessQueue = PersistentConcurrentQueue.Deserialize(qFilename);
            _logger.LogInformation(
                "File {f} found, and deserialized a queue of {n:n0} blocks to process. " +
                "This overrides the list of {x:n0} blocks set by the traverse options.",
                qFilename,
                blocksToProcessQueue.Count,
                heights.Count);
        }


        var fqFilename = options.Bitcoin.Traverse.BlocksFailedToProcessListFilename;
        if (!File.Exists(fqFilename))
        {
            failedBlocksQueue = new PersistentConcurrentQueue(fqFilename, []);
            failedBlocksQueue.Serialize();

            _logger.LogInformation(
                "File {f} not found, and initialized to an empty queue.",
                fqFilename);
        }
        else
        {
            failedBlocksQueue = PersistentConcurrentQueue.Deserialize(fqFilename);

            _logger.LogInformation(
                "File {f} found, and deserialized a queue of {n:n0} previously failed blocks.",
                fqFilename,
                failedBlocksQueue.Count);
        }

        if (failedBlocksQueue.Count > 0)
        {
            foreach (var failedBlock in failedBlocksQueue)
                blocksToProcessQueue.Enqueue(failedBlock);

            var preClearCount = failedBlocksQueue.Count;
            failedBlocksQueue.Clear();
            blocksToProcessQueue.Serialize();
            failedBlocksQueue.Serialize();

            _logger.LogInformation(
                "The {n:n0} previously failed blocks to process are added to the queue, " +
                "and the failed blocks queue is cleared.",
                preClearCount);
        }
    }

    private async Task TraverseBlocksAsync(
        Options options,
        PersistentConcurrentQueue blocksQueue,
        PersistentConcurrentQueue failedBlocksQueue,
        CancellationToken cT)
    {
        void RegisterFailed(long h)
        {
            failedBlocksQueue.Enqueue(h);
            failedBlocksQueue.Serialize();
            _logger.LogWarning("Added block {h:n0} to the list of failed blocks.", h);
        }

        var pgbSemaphore = new SemaphoreSlim(
            initialCount: options.Bitcoin.Traverse.MaxBlocksInBuffer, 
            maxCount: options.Bitcoin.Traverse.MaxBlocksInBuffer);

        // TODO: pass the bitcoin option to the following method instead of passing null values depending on the set options.
        // TODO: refactor the following so that only options is passed to the buffer

        using var gBuffer = new PersistentGraphBuffer(
            graphAgent: _host.Services.GetRequiredService<Graph.Bitcoin.BitcoinGraphAgent>(),
            logger: _host.Services.GetRequiredService<ILogger<PersistentGraphBuffer>>(),
            pTxoLifeCyccleLogger: options.Bitcoin.Traverse.TrackTxo ?_host.Services.GetRequiredService<ILogger<PersistentTxoLifeCycleBuffer>>() : null,
            txoLifeCycleFilename: options.Bitcoin.Traverse.TrackTxo ? options.Bitcoin.Traverse.TxoFilename : null,
            maxTxoPerFile: options.Bitcoin.Traverse.MaxTxoPerFile,
            options: options,
            semaphore: pgbSemaphore,
            ct: cT);

        _logger.LogInformation(
            "Traversing {count:n0} blocks in range [{from:n0}, {to:n0}].",
            blocksQueue.Count,
            blocksQueue.Min(),
            blocksQueue.Max());

        var parallelOptions = new ParallelOptions() { CancellationToken = cT };
        if (options.Bitcoin.Traverse.MaxConcurrentBlocks != null)
            parallelOptions.MaxDegreeOfParallelism =
                (int)options.Bitcoin.Traverse.MaxConcurrentBlocks;

        #if DEBUG
        parallelOptions.MaxDegreeOfParallelism = 1;
        #endif

        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, cT);

        cT.ThrowIfCancellationRequested();

        try
        {
            // Have tested TPL dataflow as alternative to Parallel.For,
            // it adds more complexity with little performance improvements,
            // and in some cases, slower than Parallel.For and sequential traversal.
            await Parallel.ForEachAsync(
                new bool[blocksQueue.Count],
                parallelOptions,
                async (_, _loopCancellationToken) =>
                {
                    pgbSemaphore.Wait(_loopCancellationToken);

                    _loopCancellationToken.ThrowIfCancellationRequested();

                    blocksQueue.TryDequeue(out var h);

                    try
                    {
                        if (!await TryProcessBlock(options, gBuffer, h, cT))
                            RegisterFailed(h);
                    }
                    catch (Exception e) when (
                        e is TaskCanceledException ||
                        e is OperationCanceledException)
                    {
                        _logger.LogWarning(
                            "Cancelled processing block {b:n0}; " +
                            "added block height to the list of blocks to process", h);
                        blocksQueue.Enqueue(h);
                        throw;
                    }
                    catch (Exception)
                    {
                        RegisterFailed(h);
                        throw;
                    }

                    _loopCancellationToken.ThrowIfCancellationRequested();
                });

            await gBuffer.WaitForBufferToEmptyAsync();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            // Do not pass the cancellation token to the following call, 
            // because we want the status file to be persisted even if the 
            // cancellation was requested.
            #pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
            await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile);
            #pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods

            var canceledBlocks = gBuffer.BlocksHeightInBuffer;
            if (canceledBlocks.Count > 0)
            {
                foreach (var item in canceledBlocks)
                    blocksQueue.Enqueue(item);
                _logger.LogInformation(
                    "Added {n} cancelled blocks to the list of blocks to process.",
                    canceledBlocks.Count);
            }

            blocksQueue.Serialize();
            _logger.LogInformation("Serialized the updated list of blocks-to-process.");
        }
    }

    private async Task<bool> TryProcessBlock(
        Options options,
        PersistentGraphBuffer gBuffer,
        long height,
        CancellationToken cT)
    {
        cT.ThrowIfCancellationRequested();

        _logger.LogInformation("Block {height:n0} {step}: Started processing", height, "[1/3]");

        var strategy = ResilienceStrategyFactory.Bitcoin.GetGraphStrategy(
            options.Bitcoin.Traverse.BitcoinAgentResilienceStrategy);

        var agent = _host.Services.GetRequiredService<BitcoinChainAgent>();
        var blockGraph = await agent.GetGraph(height, strategy, options.Bitcoin, cT);

        if (blockGraph == null)
            return false;

        _logger.LogInformation(
            "Block {height:n0} {step}: Obtained block graph and enqueued for serialization.",
            height, "[2/3]");

        // This should be the last step of this process,
        // do not check for cancellation after this.
        gBuffer.Enqueue(blockGraph);

        return true;
    }
}
