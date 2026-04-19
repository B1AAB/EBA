namespace EBA.Graph.Bitcoin;

public class PostBulkImportFinalizer(
    Options options, 
    IGraphDb<BitcoinGraph> graphDb,
    ILogger<BitcoinGraphAgent> logger)
{
    private readonly Options _options = options;
    private readonly IGraphDb<BitcoinGraph> _graphDb = graphDb;
    private readonly ILogger<BitcoinGraphAgent> _logger = logger;

    public async Task Finalize(CancellationToken ct)
    {        
        await AddSchemaAndSeeding(ct);
    }

    private async Task AddSchemaAndSeeding(CancellationToken ct)
    {
        _logger.LogInformation("Setting schema and seeding data.");

        var schemas = new List<string>();
        var seedingCommands = new List<string>();
        foreach (var nodeStrategy in _graphDb.StrategyFactory.NodeStrategies)
        {
            schemas.AddRange(nodeStrategy.Value.GetSchemaConfigs());
            seedingCommands.AddRange(nodeStrategy.Value.GetSeedingCommands());
        }
        foreach (var edgeStrategy in _graphDb.StrategyFactory.EdgeStrategies)
        {
            schemas.AddRange(edgeStrategy.Value.GetSchemaConfigs());
            seedingCommands.AddRange(edgeStrategy.Value.GetSeedingCommands());
        }

        _logger.LogInformation("Executing {count:n0} schema configuration commands.", schemas.Count);
        await _graphDb.ExecuteWriteQueryAsync(schemas, ct);

        _logger.LogInformation("Executing {count:n0} seeding commands.", seedingCommands.Count);
        await _graphDb.ExecuteWriteQueryAsync(seedingCommands, ct);

        _logger.LogInformation("Completed setting schema and seeding data.");
    }
}
