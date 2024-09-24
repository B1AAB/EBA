using BC2G.Blockchains.Bitcoin.Model;

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

    public async Task TraverseAsync(
        Options options, 
        CancellationToken cT)
    {
        var chainInfo = await _agent.AssertChainAsync(cT);
        _logger.LogInformation("Head of the chain is at block {block:n0}.", chainInfo.Blocks);
        options.Bitcoin.To ??= chainInfo.Blocks;

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

    private static PersistentConcurrentQueue SetupBlocksQueue(
        Options options)
    {
        var heights = new List<long>();
        for (int h = options.Bitcoin.From;
            h <= options.Bitcoin.To;
            h += options.Bitcoin.Granularity)
            heights.Add(h);

        return GetPersistentBlocksQueue(options.Bitcoin.BlocksToProcessListFilename, heights);
    }

    private static PersistentConcurrentQueue GetPersistentBlocksQueue(
        string filename, 
        List<long>? init = null)
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
            _host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>(),
            _host.Services.GetRequiredService<ILogger<PersistentGraphBuffer>>(),
            _host.Services.GetRequiredService<ILogger<PersistentGraphStatistics>>(),
            _host.Services.GetRequiredService<ILogger<PersistentBlockAddressess>>(),
            options.Bitcoin.StatsFilename,
            options.Bitcoin.PerBlockAddressesFilename,
            cT);

        _logger.LogInformation(
            "Traversing blocks [{from:n0}, {to:n0}).",
            options.Bitcoin.From,
            options.Bitcoin.To);

        _logger.LogInformation(
            "{count:n0} blocks to process; {processed:n0} blocks are previously processed.",
            blocksQueue.Count,
            options.Bitcoin.To - options.Bitcoin.From - blocksQueue.Count);

        var parallelOptions = new ParallelOptions() { CancellationToken = cT };
        if (options.Bitcoin.MaxConcurrentBlocks != null)
            parallelOptions.MaxDegreeOfParallelism =
                (int)options.Bitcoin.MaxConcurrentBlocks;

        #if DEBUG
        parallelOptions.MaxDegreeOfParallelism = 1;
        #endif

        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, cT);

        cT.ThrowIfCancellationRequested();
        var needFinalDbCommit = true;
        var dbContextLock = new object();
        var dbLock = new object();
        var utxos = new ConcurrentDictionary<string, Utxo>();
        var barrier = new Barrier(0, (barrier) =>
        {
            CommitInMemUtxo(utxos, dbLock, options);
            needFinalDbCommit = false;
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

                    needFinalDbCommit = true;

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
                            "Max UTXO buffer size reached (limit={b:n0}, waiting for {count} " +
                            "other concurrent tasks; Current phase {phase}.",
                            options.Bitcoin.DbCommitAtUtxoBufferSize,
                            barrier.ParticipantsRemaining, 
                            barrier.CurrentPhaseNumber);

                        barrier.SignalAndWait(cT);
                    }

                    barrier.RemoveParticipant();
                    _loopCancellationToken.ThrowIfCancellationRequested();
                });
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

            if (needFinalDbCommit)
                CommitInMemUtxo(utxos, dbLock, options);
        }
    }

    private void CommitInMemUtxo(ConcurrentDictionary<string, Utxo> utxos, object dbLock, Options options)
    {
        var retainInMemoryTxCount = options.Bitcoin.MaxInMemoryUtxosAfterDbCommit;

        _logger.LogInformation("Selecting in-memory utxo to commit in database.");
        var utxoToKeepIds = utxos.Where(kvp => kvp.Value.ReferencedInCount == 0).Select(kvp => kvp.Key).ToList();
        var utxoCount = utxoToKeepIds.Count;
        utxoToKeepIds = utxoToKeepIds.Take(retainInMemoryTxCount).ToList();
        var utxoToKeep = new ConcurrentDictionary<string, Utxo>();
        foreach (var id in utxoToKeepIds)
        {
            utxos.Remove(id, out var value);
            if (value != null)
                utxoToKeep.TryAdd(id, value);
        }
        _logger.LogInformation(
            "Selected {toKeep:n0} utxo to keep in-memory, and {toCommit:n0} txo to commit to database ({spent:n0}/{toCommit:n0} are utxo).",
            utxoToKeep.Count, utxos.Count, utxoCount - retainInMemoryTxCount, utxos.Count);

        if (options.Bitcoin.UseTxDatabase)
        {
            _logger.LogInformation("Committing the in-memory UTXO to the database.");
            DatabaseContext.OptimisticAddOrUpdate(
                dbLock,
                utxos.Values,
                _host.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>(),
                _host.Services.GetRequiredService<ILogger<DatabaseContext>>());

            _logger.LogInformation("Finished committing the in-memory UTXO to the database.");
        }

        utxos.Clear();
        utxos = utxoToKeep;
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

        _logger.LogInformation("Block {height:n0} {step}: Started processing", height, "[1/3]");

        var strategy = ResilienceStrategyFactory.Bitcoin.GetGraphStrategy(
            options.Bitcoin.BitcoinAgentResilienceStrategy);

        var agent = _host.Services.GetRequiredService<BitcoinAgent>();
        var blockGraph = await agent.GetGraph(height, utxos, options.Bitcoin.UseTxDatabase, dbContextLock, strategy, cT);

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
