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
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, cT);

        cT.ThrowIfCancellationRequested();
        var stopwatch = new Stopwatch();

        while (true)
        {
            try
            {
                stopwatch.Start();
                await TraverseBlocksAsync(options, blockHeightQueue, cT);
                stopwatch.Stop();

                if (cT.IsCancellationRequested)
                    _logger.LogInformation("Cancelled successfully.");
                else
                    _logger.LogInformation("Successfully finished traverse in {et}.", stopwatch.Elapsed);
                break;
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException e)
            {
                stopwatch.Stop();
                _logger.LogError("Circuit is broken! {e}.", e.Message);

                if (!CLI.Utilities.Prompt(cT))
                {
                    _logger.LogCritical(
                        "Aborting execution due to circuit break " +
                        "and user's decision to avoid a re-attempt.");
                    throw;
                }
            }
            catch
            {
                stopwatch.Stop();
                throw;
            }
        }
    }

    private static PersistentConcurrentQueue SetupBlocksQueue(Options options)
    {
        if (string.IsNullOrEmpty(options.Bitcoin.BlocksToProcessListFilename))
            options.Bitcoin.BlocksToProcessListFilename =
                Path.Combine(
                    options.WorkingDir,
                    $"bitcoin_blocks_to_process_in_range_" +
                    $"{options.Bitcoin.FromInclusive}" +
                    $"_to_" +
                    $"{options.Bitcoin.ToExclusive}.bc2g");

        PersistentConcurrentQueue blockHeightQueue;
        if (!File.Exists(options.Bitcoin.BlocksToProcessListFilename))
        {
            var heights = new List<int>();
            for (int h = options.Bitcoin.FromInclusive;
                h < options.Bitcoin.ToExclusive;
                h += options.Bitcoin.Granularity)
                heights.Add(h);
            blockHeightQueue = new PersistentConcurrentQueue(
                options.Bitcoin.BlocksToProcessListFilename, heights);
            blockHeightQueue.Serialize();
        }
        else
        {
            blockHeightQueue = PersistentConcurrentQueue.Deserialize(
                options.Bitcoin.BlocksToProcessListFilename);
        }

        return blockHeightQueue;
    }

    private async Task TraverseBlocksAsync(
        Options options,
        PersistentConcurrentQueue blocksQueue,
        CancellationToken cT)
    {
        using var pGraphStat = new PersistentGraphStatistics(
            Path.Combine(options.WorkingDir, "blocks_stats.tsv"), cT);

        using var gBuffer = new PersistentGraphBuffer(
            _host.Services.GetRequiredService<IGraphDb<BitcoinBlockGraph>>(),
            _host.Services.GetRequiredService<ILogger<PersistentGraphBuffer>>(),
            pGraphStat,
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
                _host.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>());
            utxos.Clear();
            _logger.LogInformation("In-memory UTXO cleared.");
        });

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
                await ProcessBlock(options, gBuffer, h, utxos, dbContextLock, cT);
                blocksQueue.Serialize();

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

        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, cT);
        blocksQueue.Serialize();

        cT.ThrowIfCancellationRequested();

        // TODO: this is not a good strategy, it has two drawbacks: 
        // - it is an infinite loop with the assumption that the
        // condition will be met eventually, but there is a chance
        // that the condition is not met in a given time, so it
        // should break with a timeout;
        // - the sleep blocks other threads, so a background task 
        // will never end.
        while (true)
        {
            if (/*mapper.CanDispose &&*/ gBuffer.CanDispose)
                break;
            Thread.Sleep(500);
        }
    }

    private async Task ProcessBlock(
        Options options,
        PersistentGraphBuffer gBuffer,
        int height,
        ConcurrentDictionary<string, Utxo> utxos,
        object dbContextLock,
        CancellationToken cT)
    {
        cT.ThrowIfCancellationRequested();

        _logger.LogInformation("Started processing block {height:n0}.", height);

        var strategy = ResilienceStrategyFactory.Bitcoin.GetGraphStrategy(
            options.Bitcoin.BitcoinAgentResilienceStrategy);

        await strategy.ExecuteAsync(async (context, _ct) =>
        {
            // Note that _ct is a linked cancellation token, linking
            // user's token and the timeout policy's cancellation token.

            _logger.LogInformation("Trying processing block {height:n0}.", height);
            var agent = _host.Services.GetRequiredService<BitcoinAgent>();
            var blockGraph = await agent.GetGraph(height, utxos, dbContextLock, _ct);

            _logger.LogInformation(
                "Obtained block graph for height {height:n0}, enqueued " +
                "for graph building and serialization.", height);
            gBuffer.Enqueue(blockGraph);
        }, new Context().SetLogger<Orchestrator>(_logger), cT);
    }
}
