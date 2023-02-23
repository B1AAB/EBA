namespace BC2G.Blockchains.Bitcoin;

public class BitcoinOrchestrator : IBlockchainOrchestrator
{
    private readonly BitcoinAgent _agent;
    private readonly ILogger<BitcoinOrchestrator> _logger;

    // TODO: check how this class can be improved without leveraging IHost.
    private readonly IHost _host;

    public BitcoinOrchestrator(
        BitcoinAgent agent,
        ILogger<BitcoinOrchestrator> logger,
        IHost host)
    {
        _agent = agent;
        _logger = logger;
        _host = host;
    }

    public async Task TraverseAsync(Options options, CancellationToken cT)
    {
        var chainInfo = await _agent.AssertChainAsync(cT);
        _logger.LogInformation("Head of the chain is at block {block:n0}.", chainInfo.Blocks);
        options.Bitcoin.ToExclusive ??= chainInfo.Blocks;

        var blockHeightQueue = SetupBlocksQueue(options);
        var failedBlocksQueue = GetPersistentBlocksQueue(options.Bitcoin.BlocksFailedToProcessListFilename);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, cT);

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

    private static PersistentConcurrentQueue SetupBlocksQueue(Options options)
    {
        var heights = new List<long>();
        for (int h = options.Bitcoin.FromInclusive;
            h < options.Bitcoin.ToExclusive;
            h += options.Bitcoin.Granularity)
            heights.Add(h);

        return GetPersistentBlocksQueue(options.Bitcoin.BlocksToProcessListFilename, heights);
    }

    private static PersistentConcurrentQueue GetPersistentBlocksQueue(string filename, List<long>? init = null)
    {
        PersistentConcurrentQueue blockHeightQueue;
        if (!File.Exists(filename))
        {
            init ??= new List<long>();
            blockHeightQueue = new PersistentConcurrentQueue(filename, init);
            blockHeightQueue.Serialize();
        }
        else
        {
            blockHeightQueue = PersistentConcurrentQueue.Deserialize(filename);
        }

        return blockHeightQueue;
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

        using var gBuffer = new PersistentGraphBuffer(
            _host.Services.GetRequiredService<IGraphDb<BlockGraph>>(),
            _host.Services.GetRequiredService<ILogger<PersistentGraphBuffer>>(),
            options.Bitcoin.StatsFilename,
            cT);

        _logger.LogInformation(
            "Traversing blocks [{from:n0}, {to:n0}).",
            options.Bitcoin.FromInclusive,
            options.Bitcoin.ToExclusive);

        _logger.LogInformation(
            "{count:n0} blocks to process; {processed:n0} blocks are previously processed.",
            blocksQueue.Count,
            options.Bitcoin.ToExclusive - options.Bitcoin.FromInclusive - blocksQueue.Count);

        var parallelOptions = new ParallelOptions() { CancellationToken = cT };
        if (options.Bitcoin.MaxConcurrentBlocks != null)
            parallelOptions.MaxDegreeOfParallelism =
                (int)options.Bitcoin.MaxConcurrentBlocks;

        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, cT);

        cT.ThrowIfCancellationRequested();
        var dbContextLock = new object();
        var dbLock = new object();
        var utxos = new ConcurrentDictionary<string, Utxo>();
        var barrier = new Barrier(0, (barrier) =>
        {
            _logger.LogInformation("Committing in-memory UTXO to database.");
            DatabaseContext.OptimisticAddOrUpdate(
                dbLock,
                utxos.Values,
                _host.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>(),
                _host.Services.GetRequiredService<ILogger<DatabaseContext>>());
            utxos.Clear();
            _logger.LogInformation("In-memory UTXO cleared.");
        });

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
                    _loopCancellationToken.ThrowIfCancellationRequested();

                    barrier.AddParticipant();
                    blocksQueue.TryDequeue(out var h);

                    try
                    {
                        if (!await TryProcessBlock(options, gBuffer, h, utxos, dbContextLock, cT))
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

                    if (utxos.Count >= options.Bitcoin.DbCommitAtUtxoBufferSize)
                    {
                        _logger.LogInformation(
                            "Max UTXO buffer size reached, waiting for {count} " +
                            "other concurrent tasks; Current phase {phase}.",
                            barrier.ParticipantsRemaining, barrier.CurrentPhaseNumber);
                        barrier.SignalAndWait(cT);
                    }

                    barrier.RemoveParticipant();
                    _loopCancellationToken.ThrowIfCancellationRequested();
                });
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
        ConcurrentDictionary<string, Utxo> utxos,
        object dbContextLock,
        CancellationToken cT)
    {
        cT.ThrowIfCancellationRequested();

        _logger.LogInformation("Started processing block {height:n0}.", height);

        var strategy = ResilienceStrategyFactory.Bitcoin.GetGraphStrategy(
            options.Bitcoin.BitcoinAgentResilienceStrategy);

        _logger.LogInformation("Trying processing block {height:n0}.", height);
        var agent = _host.Services.GetRequiredService<BitcoinAgent>();
        var blockGraph = await agent.GetGraph(height, utxos, dbContextLock, cT, strategy);

        if (blockGraph == null)
            return false;

        _logger.LogInformation(
            "Obtained block graph for height {height:n0}, " +
            "enqueued for graph serialization.", height);

        // This should be the last step of this process,
        // do not check for cancellation after this.
        gBuffer.Enqueue(blockGraph);

        return true;
    }
}
