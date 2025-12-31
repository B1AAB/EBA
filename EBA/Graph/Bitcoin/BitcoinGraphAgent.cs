using EBA.Graph.Bitcoin.TraversalAlgorithms;

namespace EBA.Graph.Bitcoin;

public class BitcoinGraphAgent : IGraphAgent<BitcoinGraph>, IDisposable
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _db;
    private readonly ILogger<BitcoinGraphAgent> _logger;

    private bool _hasSerializedConstants = false;

    private bool _disposed = false;

    public BitcoinGraphAgent(
        Options options,
        IGraphDb<BitcoinGraph> graphDb,
        ILogger<BitcoinGraphAgent> logger)
    {
        _options = options;
        _db = graphDb;
        _logger = logger;
    }

    public async Task SampleAsync(CancellationToken ct)
    {
        var sampler = _options.Bitcoin.GraphSample.TraversalAlgorithm switch
        {
            GraphTraversal.FFS => new ForestFire(_options, _db, _logger),
            GraphTraversal.BFS => throw new NotImplementedException(),
            GraphTraversal.DFS => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };

        await sampler.SampleAsync(ct);
    }

    public async Task SerializeAsync(BitcoinGraph g, CancellationToken ct)
    {
        if (!_hasSerializedConstants)
        {
            await _db.SerializeConstantsAsync(ct);
            _hasSerializedConstants = true;
        }

        await _db.SerializeAsync(g, ct);
    }

    /// <summary>
    /// Converts a specified graph component type to its corresponding node label.
    /// 
    /// This is a temp utility method until the node lables and graph component types enums are unified.
    /// </summary>
    public static NodeLabels ConvertGraphComponentTypeToNodeLabel(GraphComponentType type)
    {
        return type switch
        {
            GraphComponentType.BitcoinBlockNode => NodeLabels.Block,
            GraphComponentType.BitcoinScriptNode => NodeLabels.Script,
            GraphComponentType.BitcoinTxNode => NodeLabels.Tx,
            GraphComponentType.BitcoinCoinbaseNode => NodeLabels.Coinbase,
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Not a valid node type: {type}"),
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _db.Dispose();
            }

            _disposed = true;
        }
    }
}