using EBA.Graph.Bitcoin.Factories;
using EBA.Graph.Bitcoin.Strategies;
using EBA.Utilities;

namespace EBA.Graph.Bitcoin.OffChain;

public class EconomicAugmentor(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphAgent> logger)
{
    private readonly Options _options = options;
    private readonly IGraphDb<BitcoinGraph> _graphDb = graphDb;
    private readonly ILogger<BitcoinGraphAgent> _logger = logger;

    public async Task SetBlockMarketIndicators(CancellationToken ct)
    {
        if (!OHLCV.TryParseFile(_options.Bitcoin.Augmentor.BlockOhlcvMappedFilename, out var blockOHLCVMapping))
        {
            _logger.LogError(
                "Failed to parse OHLCV data from file: {filename}",
                _options.Bitcoin.Augmentor.BlockOhlcvMappedFilename);
            return;
        }
        else
        {
            _logger.LogInformation(
                "Successfully parsed OHLCV data for {count:n0} blocks from file: {filename}",
                blockOHLCVMapping.Count,
                _options.Bitcoin.Augmentor.BlockOhlcvMappedFilename);
        }

        var blockNodes = await GetBlockNodes(ct);

        foreach (var block in blockNodes)
            if (blockOHLCVMapping.TryGetValue(block.Key, out var ohlcv))
                block.Value.BlockMetadata.Ohlcv = ohlcv;        

        await ComputeBlockValuationMetrics(blockNodes, blockOHLCVMapping, ct);
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

    // TODO: this can be set as part of the pre-bulk import finalization step,
    // instead of being computed and updated after the fact.
    // This would require reordering the import steps to ensure that the OHLCV data is available before the block nodes are created.
    private async Task ComputeBlockValuationMetrics(
        SortedDictionary<long, BlockNode> blocks, 
        Dictionary<long, OHLCV> blockOHLCVMapping, 
        CancellationToken ct)
    {
        _logger.LogInformation("Setting realized cap for {count:n0} block nodes.", blocks.Count);
        await _graphDb.SetRealizedCap(blocks, blockOHLCVMapping, CancellationToken.None);

        _logger.LogInformation("Saving realized cap for {count:n0} block nodes.", blocks.Count);
        var updates = blocks.Values
            .Select(b => BlockNodeStrategy.EconomicMappings.ToDictionary(b)) // TODO: update this
            .ToList();

        await _graphDb.BulkUpdateNodePropertiesAsync(
            NodeKind.Block,
            nameof(BlockMetadata.Height),
            updates,
            ct);
    }
}
