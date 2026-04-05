using EBA.Graph.Bitcoin.Factories;

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

        await SetSupplyAmount(ct);

        await _graphDb.SetUTxOSpentHeight(ct);
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

    private async Task SetSupplyAmount(CancellationToken ct)
    {
        _logger.LogInformation("Setting total supply feature.");

        _logger.LogInformation("Fetching blocks from graph database.");
        var nodeVar = "n";
        var records = await _graphDb.GetNodesAsync(NodeKind.Block, CancellationToken.None, nodeVariable: nodeVar);
        _logger.LogInformation("Retrieved {count:n0} records from graph database. Creating block nodes.", records.Count);

        NodeFactory.TryCreate<BlockNode>(records, out var blockNodes, nodeVar);

        var blocks = new SortedList<long, BlockNode>();
        foreach (var block in blockNodes)
            blocks.Add(block.BlockMetadata.Height, block);        

        if (blocks.First().Value.BlockMetadata.Height != 0)
        {
            throw new InvalidOperationException(
                $"The first block in the graph has height " +
                $"{blocks.First().Value.BlockMetadata.Height:,}, " +
                $"expected 0.");
        }

        if (blocks.Last().Value.BlockMetadata.Height != blocks.Count - 1)
        {
            throw new InvalidOperationException(
                $"This operation requires a continues set of blocks, there are missing blocks");
        }

        blocks[0].BlockMetadata.TotalSupply = blocks[0].BlockMetadata.MintedBitcoins;
        blocks[0].BlockMetadata.TotalSupplyNominal = blocks[0].BlockMetadata.TotalSupply;
        for (var i = 1; i < blocks.Count; i++)
        {
            blocks[i].BlockMetadata.TotalSupply =
                blocks[i - 1].BlockMetadata.TotalSupply +
                blocks[i].TripletTypeValueSum[C2TEdge.Kind] -
                blocks[i].BlockMetadata.ProvablyUnspendableBitcoins;

            blocks[i].BlockMetadata.TotalSupplyNominal =
                blocks[i - 1].BlockMetadata.TotalSupplyNominal + blocks[i].BlockMetadata.MintedBitcoins;
        }

        var updates = blocks.Values.Select(b => new Dictionary<string, object?>
        {
            [nameof(BlockMetadata.Height)] = b.BlockMetadata.Height,
            [nameof(BlockMetadata.TotalSupply)] = b.BlockMetadata.TotalSupply,
            [nameof(BlockMetadata.TotalSupplyNominal)] = b.BlockMetadata.TotalSupplyNominal,
        }).ToList();

        _logger.LogInformation("Pushing {count:n0} block updates to graph database.", updates.Count);
        await _graphDb.BulkUpdateNodePropertiesAsync(
            NodeKind.Block,
            nameof(BlockMetadata.Height),
            updates,
            ct);
        _logger.LogInformation("Completed pushing block updates.");

        _logger.LogInformation("Completed setting total supply feature.");
    }
}
