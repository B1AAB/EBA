using EBA.Graph.Bitcoin.Factories;

namespace EBA.Graph.Bitcoin;

public class PostProcess
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<BitcoinGraphAgent> _logger;

    public PostProcess(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphAgent> logger)
    {
        _options = options;
        _graphDb = graphDb;
        _logger = logger;
    }

    public async Task Run()
    {
        var blocks = new SortedList<long, BlockNode>();
        var nodeVar = "n";

        _logger.LogInformation("Fetching blocks from graph database.");
        var records = await _graphDb.GetNodesAsync(NodeKind.Block, CancellationToken.None, nodeVariable: nodeVar);
        _logger.LogInformation("Retrieved {count:n0} records from graph database. Creating block nodes.", records.Count);

        foreach (var record in records)
        {
            NodeFactory.TryCreate(record[nodeVar].As<Neo4j.Driver.INode>(), out var blockNode);
            var block = (BlockNode)blockNode;
            blocks.Add(block.BlockMetadata.Height, block);
        }

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
                blocks[i].BlockMetadata.MintedBitcoins -
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
            CancellationToken.None);
        _logger.LogInformation("Completed pushing block updates.");
    }
}
