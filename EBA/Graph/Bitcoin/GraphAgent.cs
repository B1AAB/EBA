using EBA.Graph.Bitcoin.TraversalAlgorithms;
using EBA.Utilities;

namespace EBA.Graph.Bitcoin;

public class GraphAgent : IGraphAgent
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _db;
    private readonly ILogger<GraphAgent> _logger;


    public GraphAgent(
        Options options,
        IGraphDb<BitcoinGraph> graphDb,
        ILogger<GraphAgent> logger)
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
}