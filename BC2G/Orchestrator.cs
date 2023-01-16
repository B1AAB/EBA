using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BC2G;

public class Orchestrator : IDisposable
{
    private readonly CLI _cli;
    private readonly IHost _host;
    private readonly Options _options;
    private readonly CancellationToken _cT;

    private bool disposed = false;

    public ILogger Logger { get; }

    public Orchestrator(IHost host, Options options, CancellationToken cancelationToken)
    {
        _host = host;
        _cT = cancelationToken;
        _options = options;
        Logger = _host.Services.GetRequiredService<ILogger<Orchestrator>>();
        _cli = new CLI(
            _options,
            TraverseAsync,
            SampleGraph,
            LoadGraphAsync,
            (e, c) => { Logger?.LogCritical("{error}", e.Message); });
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        return await _cli.InvokeAsync(args);
    }

    private async Task SampleGraph()
    {
        await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile, _cT);
        var graphDb = _host.Services.GetRequiredService<GraphDB>();
        await graphDb.Sampling();
    }

    private async Task LoadGraphAsync()
    {
        await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile, _cT);
        var graphDb = _host.Services.GetRequiredService<GraphDB>();
        graphDb.BulkImport(_options.WorkingDir);
    }

    private async Task TraverseAsync()
    {
        await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile, _cT);

        var agent = _host.Services.GetRequiredService<BitcoinAgent>();
        var chainInfo = await agent.AssertChainAsync(_cT);

        _cT.ThrowIfCancellationRequested();
        using (var dbContext = _host.Services.GetRequiredService<DatabaseContext>())
            await dbContext.Database.EnsureCreatedAsync(_cT);

        if (_options.Bitcoin.FromInclusive == null)
            _options.Bitcoin.FromInclusive = _options.Bitcoin.LastProcessedBlock + 1;
        if (_options.Bitcoin.ToExclusive == null)
            _options.Bitcoin.ToExclusive = chainInfo.Blocks;

        await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile, _cT);

        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();
            await TraverseBlocksAsync(_options, _cT);
            stopwatch.Stop();
            if (_cT.IsCancellationRequested)
                Logger.LogInformation("Cancelled successfully.");
            else
                Logger.LogInformation($"All process finished successfully in {stopwatch.Elapsed}.");
        }
        catch
        {
            stopwatch.Stop();
            throw;
        }
    }

    private async Task TraverseBlocksAsync(Options options, CancellationToken cT)
    {
        using var pGraphStat = new PersistentGraphStatistics(
            Path.Combine(options.WorkingDir, "blocks_stats.tsv"),
            cT);

        using var gBuffer = new PersistentGraphBuffer(
            _host.Services.GetRequiredService<GraphDB>(),
            _host.Services.GetRequiredService<ILogger<PersistentGraphBuffer>>(),            
            pGraphStat,
            cT);

        Logger.LogInformation(
            "Traversing blocks [{from}, {to}).", 
            options.Bitcoin.FromInclusive, 
            options.Bitcoin.ToExclusive);

        var blockHeightQueue = new ConcurrentQueue<int>();
        for (int h = options.Bitcoin.LastProcessedBlock?? 0 + 1;
                 h < options.Bitcoin.ToExclusive;
                 h += options.Bitcoin.Granularity)
            blockHeightQueue.Enqueue(h);

        var parallelOptions = new ParallelOptions()
        { CancellationToken = cT };

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
            Logger.LogInformation("Committing in-memory UTXO to database.");
            DatabaseContext.OptimisticAddOrUpdate(
                dbLock,
                utxos.Values,
                _host.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>());
            utxos.Clear();
            Logger.LogInformation("In-memory UTXO cleared.");
        });

        // Have tested TPL dataflow as alternative to Parallel.For,
        // it adds more complexity with little performance improvements,
        // and in some cases, slower than Parallel.For and sequential traversal.
        await Parallel.ForEachAsync(
            new bool[blockHeightQueue.Count],
            parallelOptions,
            async (_, _loopCancellationToken) =>
            {
                _loopCancellationToken.ThrowIfCancellationRequested();

                barrier.AddParticipant();
                blockHeightQueue.TryDequeue(out var h);
                //Logger.LogStartProcessingBlock(h);
                await ProcessBlock(options, gBuffer, /*serializer,*/ h, utxos, dbContextLock, /*individualBlocksDir,*/ cT);

                if (utxos.Count >= options.Bitcoin.DbCommitAtUtxoBufferSize)
                {
                    Logger.LogInformation(
                        "Max UTXO buffer size reached, waiting for {count} " +
                        "other concurrent tasks; Current phase {phase}.", 
                        barrier.ParticipantsRemaining, barrier.CurrentPhaseNumber);
                    barrier.SignalAndWait(cT);
                }

                barrier.RemoveParticipant();
                _loopCancellationToken.ThrowIfCancellationRequested();
            });

        //graphDb.FinishBulkImport();

        //Logger.LogFinishTraverse(cancellationToken.IsCancellationRequested);

        // At this method's exist, the dispose method of
        // the types wrapped in `using` will be called that
        // finalizes persisting output.
        //Logger.Log("Finalizing serialized files.");
        await JsonSerializer<Options>.SerializeAsync(_options, options.StatusFile, cT);

        cT.ThrowIfCancellationRequested();

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

    private async Task ProcessBlock(
        Options options,
        PersistentGraphBuffer gBuffer,
        int height,
        ConcurrentDictionary<string, Utxo> utxos,
        object dbContextLock,
        CancellationToken cT)
    {
        cT.ThrowIfCancellationRequested();

        Logger.LogInformation("Started processing block {height}.", height);

        var strategy = ResilienceStrategyFactory.Bitcoin.GetGraphStrategy(
            options.Bitcoin.BitcoinAgentResilienceStrategy);

        await strategy.ExecuteAsync(async (context, _ct) =>
        {
            // Note that _ct is linked cancellation token, linking
            // user's token and the timeout policy's cancellation token.

            Logger.LogInformation("Trying processing block {height}.", height);
            var agent = _host.Services.GetRequiredService<BitcoinAgent>();
            var blockGraph = await agent.GetGraph(height, utxos, dbContextLock, _ct);

            Logger.LogInformation(
                "Obtained block graph for height {height}, enqueued " +
                "for graph building and serialization.", height);
            gBuffer.Enqueue(blockGraph);
        }, new Context().SetLogger<Orchestrator>(Logger), cT);
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
                //Log.CloseAndFlush();
                //Logger.Dispose();
                //_txCache.Dispose();
            }

            disposed = true;
        }
    }
}
