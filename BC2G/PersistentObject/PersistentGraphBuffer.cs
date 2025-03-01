using BC2G.Utilities;

namespace BC2G.PersistentObject;

public class PersistentGraphBuffer : PersistentObjectBase<BlockGraph>, IDisposable
{
    private readonly IGraphDb<BitcoinGraph>? _graphDb;
    private readonly ILogger<PersistentGraphBuffer> _logger;
    private readonly PersistentGraphStatistics _pGraphStats;
    private readonly PersistentBlockAddresses _pBlockAddresses;
    private readonly PersistentTxoLifeCycleBuffer? _pTxoLifeCycleBuffer = null;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed = false;
    private readonly Options _options;

    public ReadOnlyCollection<long> BlocksHeightInBuffer
    {
        get
        {
            return new ReadOnlyCollection<long>([.. _blocksHeightsInBuffer.Keys]);
        }
    }
    private readonly ConcurrentDictionary<long, byte> _blocksHeightsInBuffer = new();

    public PersistentGraphBuffer(
        IGraphDb<BitcoinGraph>? graphDb,
        ILogger<PersistentGraphBuffer> logger,
        ILogger<PersistentGraphStatistics> pgStatsLogger,
        ILogger<PersistentBlockAddresses> pgAddressesLogger,
        ILogger<PersistentTxoLifeCycleBuffer>? pTxoLifeCyccleLogger,
        string graphStatsFilename,
        string perBlockAddressesFilename,
        string? txoLifeCycleFilename,
        int maxTxoPerFile,
        int maxAddressesPerFile,
        SemaphoreSlim semaphore,
        Options options,
        CancellationToken ct) :
        base(logger, ct)
    {
        _graphDb = graphDb;
        _logger = logger;

        _options = options;

        _pGraphStats = new(graphStatsFilename, int.MaxValue, pgStatsLogger, ct);
        _pBlockAddresses = new(perBlockAddressesFilename, maxAddressesPerFile, pgAddressesLogger, ct);

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
        var tasks = new List<Task>
        {
            _pGraphStats.SerializeAsync(obj.Stats.ToString(), default),
        };

        if (_graphDb != null)
            tasks.Add(_graphDb.SerializeAsync(obj, default));

        if (_pTxoLifeCycleBuffer != null)
            tasks.Add(_pTxoLifeCycleBuffer.SerializeAsync(obj.Block.TxoLifecycle.Values, default));

        if (!_options.Bitcoin.SkipSerializingAddresses)
            tasks.Add(_pBlockAddresses.SerializeAsync(obj.Stats.ToStringsAddresses(), default));

        await Task.WhenAll(tasks);

        _blocksHeightsInBuffer.TryRemove(obj.Block.Height, out byte _);

        _logger.LogInformation(
            "Block {height:n0} {step}: Finished processing in {runtime} seconds.",
            obj.Block.Height, "[3/3]", Helpers.GetEtInSeconds(obj.Stats.Runtime));

        _semaphore.Release();
    }

    public override Task SerializeAsync(IEnumerable<BlockGraph> objs, CancellationToken cT)
    {
        throw new NotImplementedException();
    }

    public int GetBufferSize()
    {
        return _blocksHeightsInBuffer.Count;
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
                _graphDb?.Dispose();
                //_pGraphStats.Dispose();
            }

            _disposed = true;
        }
    }
}
