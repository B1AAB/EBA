using EBA.Graph.Bitcoin.TraversalAlgorithms;
using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

namespace EBA.Graph.Bitcoin;

public class BitcoinGraphAgent : IGraphAgent<BitcoinGraph>, IDisposable
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _db;
    private readonly ILogger<BitcoinGraphAgent> _logger;

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
        var sampler = _options.GraphSample.TraversalAlgorithm switch
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
        using var strategy = new BitcoinStrategyFactory(_options);
        await _db.SerializeAsync(g, strategy, ct);
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
            { }

            _disposed = true;
        }
    }
}