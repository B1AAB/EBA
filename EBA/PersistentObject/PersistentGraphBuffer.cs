using EBA.Utilities;

namespace EBA.PersistentObject;

public class PersistentGraphBuffer : PersistentObjectBase<BlockGraph>, IDisposable, IAsyncDisposable
{
    private readonly Graph.Bitcoin.BitcoinGraphOrchestrator? _graphOrchestrator;
    private readonly ILogger<PersistentGraphBuffer> _logger;
    private readonly PersistentTxoLifeCycleBuffer? _pTxoLifeCycleBuffer = null;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed = false;

    public ReadOnlyCollection<long> BlocksHeightInBuffer
    {
        get
        {
            return new ReadOnlyCollection<long>([.. _blocksHeightsInBuffer.Keys]);
        }
    }
    private readonly ConcurrentDictionary<long, byte> _blocksHeightsInBuffer = new();

    public PersistentGraphBuffer(
        Graph.Bitcoin.BitcoinGraphOrchestrator? graphOrchestrator,
        ILogger<PersistentGraphBuffer> logger,
        ILogger<PersistentTxoLifeCycleBuffer>? pTxoLifeCyccleLogger,
        string? txoLifeCycleFilename,
        int maxTxoPerFile,
        SemaphoreSlim semaphore,
        Options options,
        CancellationToken ct) :
        base(logger, ct)
    {
        _graphOrchestrator = graphOrchestrator;
        _logger = logger;

        if (txoLifeCycleFilename != null && pTxoLifeCyccleLogger != null)
            _pTxoLifeCycleBuffer = new(txoLifeCycleFilename, maxTxoPerFile, pTxoLifeCyccleLogger, ct);

        _semaphore = semaphore;
    }

    public new void Enqueue(BlockGraph graph)
    {
        _blocksHeightsInBuffer.TryAdd(graph.Block.Height, 0);
        base.Enqueue(graph);
    }

    public override async Task SerializeAsync(
        BlockGraph obj,
        CancellationToken cT)
    {
        cT.ThrowIfCancellationRequested();

        // Using `default` as a cancellation token in the following
        // because the two serialization methods need to conclude before
        // this can exit, otherwise, it may end up partially persisting graph 
        // or persisting graph but skipping the serialization of its stats.
        // A better alternative for this is using roll-back approaches 
        // on cancellation and recovery, but that can add additional complexities.
        var tasks = new List<Task> { };

        if (_graphOrchestrator != null)
            tasks.Add(_graphOrchestrator.SerializeAsync(obj, default));

        if (_pTxoLifeCycleBuffer != null)
            tasks.Add(_pTxoLifeCycleBuffer.SerializeAsync(obj.Block.TxoLifecycle.Values, default));

        await Task.WhenAll(tasks);

        _blocksHeightsInBuffer.TryRemove(obj.Block.Height, out byte _);

        _logger.LogInformation(
            "Block {height:n0} {step}: Finished processing in {runtime} seconds.",
            obj.Block.Height, "[3/3]", Helpers.GetEtInSeconds(obj.Runtime));

        _semaphore.Release();
    }

    public override Task SerializeAsync(IEnumerable<BlockGraph> objs, CancellationToken cT)
    {
        throw new NotImplementedException("SerializeAsync for multiple BlockGraphs is not implemented.");
    }

    public int GetBufferSize()
    {
        return _blocksHeightsInBuffer.Count;
    }

    public async Task WaitForBufferToEmptyAsync()
    {
        // TODO: this is a naive implementation and
        // need a more efficient re-implementation. 
        while (GetBufferSize() > 0)
            await Task.Delay(50);
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual new void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _graphOrchestrator?.Dispose();
            }

            _disposed = true;
        }
    }

    public new async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            base.Dispose(true);

            if (_graphOrchestrator is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
