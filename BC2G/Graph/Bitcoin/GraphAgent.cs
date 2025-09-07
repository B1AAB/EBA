
using BC2G.Utilities;

namespace BC2G.Graph.Bitcoin;

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
        var baseOutputDir = Path.Join(_options.WorkingDir, $"sampled_graphs_{Helpers.GetUnixTimeSeconds()}");

        // TODO: if sampling method is forest fire:
        var sampler = new Samplers.ForestFire(_options, _db, _logger);
        await sampler.SampleAsync(ct);


        // TODO: maybe define a sampler factory.
    }
}
