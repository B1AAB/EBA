using EBA.Graph.Bitcoin.Factories;
using EBA.Utilities;

namespace EBA.Graph.Bitcoin.OffChain;

public class Augmentor(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphAgent> logger)
{
    private readonly Options _options = options;
    private readonly IGraphDb<BitcoinGraph> _graphDb = graphDb;
    private readonly ILogger<BitcoinGraphAgent> _logger = logger;

    public async Task AddMarketData(CancellationToken ct)
    {
        OHLCV.TryParseFile(_options.Bitcoin.Augmentor.BlockOhlcvMappedFilename, out var blockOHLCVMapping);

        var blockNodes = await GetBlockNodes(ct);

        await SetRealizedCap(blockNodes, blockOHLCVMapping, ct);
    }

    private async Task<SortedDictionary<long, BlockNode>> GetBlockNodes(CancellationToken ct)
    {
        _logger.LogInformation("Fetching block nodes.");
        var blockRecords = await _graphDb.GetNodesAsync(NodeKind.Block, ct, nodeVariable: "b");
        _logger.LogInformation("Fetched {count:n0} block nodes.", blockRecords.Count);

        var blocks = new SortedDictionary<long, BlockNode>();
        if (NodeFactory.TryCreate<BlockNode>(blockRecords, out var blockNodes, nodeVar: "b"))
        {
            foreach (var blockNode in blockNodes)
                blocks.Add(blockNode.BlockMetadata.Height, blockNode);
        }
        else
        {
            throw new Exception("Invalid node types received from database");
        }

        return blocks;
    }

    private async Task SetRealizedCap(
        SortedDictionary<long, BlockNode> blocks, 
        Dictionary<long, OHLCV> blockOHLCVMapping, 
        CancellationToken ct)
    {
        _logger.LogInformation("Setting realized cap for {count:n0} block nodes.", blocks.Count);
        await _graphDb.SetRealizedCap(blocks, blockOHLCVMapping, CancellationToken.None);

        _logger.LogInformation("Saving realized cap for {count:n0} block nodes.", blocks.Count);
        var updates = blocks.Values
            .Select(b => new Dictionary<string, object?>
            {
                [nameof(BlockMetadata.Height)] = b.BlockMetadata.Height,
                [nameof(BlockMetadata.RealizedCap)] = b.BlockMetadata.RealizedCap
            })
            .ToList();

        await _graphDb.BulkUpdateNodePropertiesAsync(
            NodeKind.Block,
            nameof(BlockMetadata.Height),
            updates,
            ct);
    }
}
