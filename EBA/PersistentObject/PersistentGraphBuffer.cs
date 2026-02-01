using EBA.Utilities;

namespace EBA.PersistentObject;

public class PersistentGraphBuffer : PersistentObjectBase<BlockGraph>, IDisposable
{
    private readonly Graph.Bitcoin.BitcoinGraphAgent? _graphAgent;
    private readonly ILogger<PersistentGraphBuffer> _logger;
    private readonly PersistentBlockAddresses? _pBlockAddresses;
    private readonly PersistentTxoLifeCycleBuffer? _pTxoLifeCycleBuffer = null;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed = false;
    private readonly Options _options;

    private const char _delimiter = '\t';

    public ReadOnlyCollection<long> BlocksHeightInBuffer
    {
        get
        {
            return new ReadOnlyCollection<long>([.. _blocksHeightsInBuffer.Keys]);
        }
    }
    private readonly ConcurrentDictionary<long, byte> _blocksHeightsInBuffer = new();

    public PersistentGraphBuffer(
        Graph.Bitcoin.BitcoinGraphAgent? graphAgent,
        ILogger<PersistentGraphBuffer> logger,
        ILogger<PersistentBlockAddresses> pgAddressesLogger,
        ILogger<PersistentTxoLifeCycleBuffer>? pTxoLifeCyccleLogger,
        string perBlockAddressesFilename,
        string? txoLifeCycleFilename,
        int maxTxoPerFile,
        int maxAddressesPerFile,
        SemaphoreSlim semaphore,
        Options options,
        CancellationToken ct) :
        base(logger, ct)
    {
        _graphAgent = graphAgent;
        _logger = logger;

        _options = options;

        if (!_options.Bitcoin.Traverse.SkipSerializingAddresses)
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
        var tasks = new List<Task> { };

        if (_graphAgent != null)
            tasks.Add(_graphAgent.SerializeAsync(obj, default));

        if (_pTxoLifeCycleBuffer != null)
            tasks.Add(_pTxoLifeCycleBuffer.SerializeAsync(obj.Block.TxoLifecycle.Values, default));

        //if (!_options.Bitcoin.SkipSerializingAddresses)
        if (_pBlockAddresses != null)
            tasks.Add(_pBlockAddresses.SerializeAsync(obj.Block.ToStringsAddresses(_delimiter), default));

        await Task.WhenAll(tasks);

        _blocksHeightsInBuffer.TryRemove(obj.Block.Height, out byte _);

        _logger.LogInformation(
            "Block {height:n0} {step}: Finished processing in {runtime} seconds.",
            obj.Block.Height, "[3/3]", Helpers.GetEtInSeconds(obj.Runtime));

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
                _graphAgent?.Dispose();
            }

            _disposed = true;
        }
    }
}
