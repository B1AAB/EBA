namespace BC2G.PersistentObject;

public class PersistentGraphBuffer : PersistentObjectBase<BitcoinBlockGraph>, IDisposable
{
    private readonly IGraphDb<BitcoinBlockGraph> _graphDb;
    private readonly ILogger<PersistentGraphBuffer> _logger;
    private readonly PersistentGraphStatistics _pGraphStats;

    private bool _disposed = false;

    public PersistentGraphBuffer(
        IGraphDb<BitcoinBlockGraph> graphDb,
        ILogger<PersistentGraphBuffer> logger,
        PersistentGraphStatistics pGraphStats,
        CancellationToken ct) : 
        base(ct)
    {
        _graphDb = graphDb;
        _pGraphStats = pGraphStats;
        _logger = logger;
    }

    public override async Task SerializeAsync(
        BitcoinBlockGraph obj,
        CancellationToken cT)
    {
        obj.MergeQueuedTxGraphs(cT);
        await _graphDb.SerializeAsync(obj, cT);

        obj.Stats.StopStopwatch();
        _pGraphStats.Enqueue(obj.Stats.ToString());

        _logger.LogInformation(
            "Finished processing block {height:n0} in {runtime}.",
            obj.Height, obj.Stats.Runtime);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //_graphDB?.FinishBulkImport();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
