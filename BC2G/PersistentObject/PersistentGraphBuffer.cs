namespace BC2G.PersistentObject;

public class PersistentGraphBuffer : PersistentObjectBase<BlockGraph>, IDisposable
{
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<PersistentGraphBuffer> _logger;
    private readonly PersistentGraphStatistics _pGraphStats;
    private bool _disposed = false;

    public ReadOnlyCollection<long> BlocksHeightInBuffer
    {
        get
        {
            return new ReadOnlyCollection<long>(_blocksHeightsInBuffer.Keys.ToArray());
        }
    }
    private readonly ConcurrentDictionary<long, byte> _blocksHeightsInBuffer = new();

    public PersistentGraphBuffer(
        IGraphDb<BitcoinGraph> graphDb,
        ILogger<PersistentGraphBuffer> logger,
        string graphStatsFilename,
        CancellationToken ct) :
        base(ct)
    {
        _graphDb = graphDb;
        _logger = logger;
        _pGraphStats = new(graphStatsFilename, ct);
    }

    public new void Enqueue(BlockGraph graph)
    {
        _blocksHeightsInBuffer.TryAdd(graph.Height, 0);
        base.Enqueue(graph);
    }

    public override async Task SerializeAsync(
        BlockGraph obj,
        CancellationToken cT)
    {
        cT.ThrowIfCancellationRequested();

        // I am using `default` as a cancellation token in the following
        // because the two serialization methods need to conclude before
        // this can exit, otherwise, it may end up partially persisting graph 
        // or persisting graph but skipping the serialization of its stats.
        // A better alternative for this is using roll-back approaches 
        // on cancellation and recovery, but that can add additional 
        // complexities not essential at this point.
        await _graphDb.SerializeAsync(obj, default);
        obj.Stats.StopStopwatch();
        await _pGraphStats.SerializeAsync(obj.Stats.ToString(), default);

        _blocksHeightsInBuffer.TryRemove(obj.Height, out byte _);

        _logger.LogInformation(
            "Finished processing block {height:n0} in {runtime}.",
            obj.Height, obj.Stats.Runtime);
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
                _pGraphStats.Dispose();
            }

            _disposed = true;
        }
    }
}
