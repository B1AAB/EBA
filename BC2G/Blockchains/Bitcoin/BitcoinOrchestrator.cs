using BC2G.Graph.Db;

using System;

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
            init ??= [];
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

        var pgbSemaphore = new SemaphoreSlim(
            initialCount: options.Bitcoin.MaxBlocksInBuffer, 
            maxCount: options.Bitcoin.MaxBlocksInBuffer);

        // TODO: pass the bitcoin option to the following method instead of passing null values depending on the set options.

        using var gBuffer = new PersistentGraphBuffer(
            graphDb: options.Bitcoin.SkipGraphSerialization ? null : _host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>(),
            logger: _host.Services.GetRequiredService<ILogger<PersistentGraphBuffer>>(),
            pgStatsLogger: _host.Services.GetRequiredService<ILogger<PersistentGraphStatistics>>(),
            pgAddressessLogger: _host.Services.GetRequiredService<ILogger<PersistentBlockAddressess>>(),
            pTxoLifeCyccleLogger: options.Bitcoin.TrackTxo ?_host.Services.GetRequiredService<ILogger<PersistentTxoLifeCycleBuffer>>() : null,
            graphStatsFilename: options.Bitcoin.StatsFilename,
            perBlockAddressessFilename: options.Bitcoin.PerBlockAddressesFilename,
            txoLifeCycleFilename: options.Bitcoin.TrackTxo ? options.Bitcoin.TxoFilename : null,
            semaphore: pgbSemaphore,
            ct: cT);

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
            options.Bitcoin.BitcoinAgentResilienceStrategy);

        var agent = _host.Services.GetRequiredService<BitcoinAgent>();
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
