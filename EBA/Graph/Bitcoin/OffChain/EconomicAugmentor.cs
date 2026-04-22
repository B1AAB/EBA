using EBA.Utilities;

namespace EBA.Graph.Bitcoin.OffChain;

public class EconomicAugmentor(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphOrchestrator> logger)
{
    private readonly Options _options = options;
    private readonly IGraphDb<BitcoinGraph> _graphDb = graphDb;
    private readonly ILogger<BitcoinGraphOrchestrator> _logger = logger;

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
        await ComputeThermocap(blockNodes, blockOHLCVMapping, ct);
        await SaveChanges(blockNodes, ct);
    }

    private async Task<SortedDictionary<long, BlockNode>> GetBlockNodes(CancellationToken ct)
    {
        _logger.LogInformation("Fetching block nodes.");
        var blockRecords = await _graphDb.GetNodesAsync(NodeKind.Block, ct, nodeVariable: "b");
        _logger.LogInformation("Fetched {count:n0} block nodes.", blockRecords.Count);

        var blocks = new SortedDictionary<long, BlockNode>();
        if (_graphDb.StrategyFactory.TryCreateNodes<BlockNode>(blockRecords, out var blockNodes, nodeVar: "b"))
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
        SortedDictionary<long, BlockNode> blockNodes,
        Dictionary<long, OHLCV> blockOHLCVMapping,
        CancellationToken ct)
    {
        _logger.LogInformation("Setting realized cap for {count:n0} block nodes.", blockNodes.Count);
        await _graphDb.SetRealizedCap(blockNodes, blockOHLCVMapping, CancellationToken.None);
    }

    private async Task ComputeThermocap(
        SortedDictionary<long, BlockNode> blockNodes,
        Dictionary<long, OHLCV> blockOHLCVMapping,
        CancellationToken ct)
    {
        foreach (var block in blockNodes)
        {
            if (block.Key == 0)
                continue;

            if (blockOHLCVMapping.TryGetValue(block.Value.BlockMetadata.Height, out var ohlcv))
            {
                block.Value.BlockMetadata.Thermocap = 
                    blockNodes[block.Key - 1].BlockMetadata.Thermocap + 
                    ohlcv.GetFiatValue(block.Value.TripletTypeValueSum[C2TEdge.Kind]);
            }
        }
    }

    private async Task SaveChanges(SortedDictionary<long, BlockNode> blockNodes, CancellationToken ct)
    {
        var economicMappings = new ElementMapper<BlockNode>(
            new MappingBuilder<BlockNode>()
                .Map(n => n.BlockMetadata.Height)
                .Map(n => (double?)n.BlockMetadata.RealizedCap)
                .Map(n => (double?)n.BlockMetadata.MarketCap)
                .Map(n => (double?)n.BlockMetadata.NUPL)
                .Map(n => (double?)n.BlockMetadata.NUL)
                .Map(n => (double?)n.BlockMetadata.NUP)
                .Map(n => (double?)n.BlockMetadata.MVRV)
                .Map(n => (double?)n.BlockMetadata.Thermocap)
                .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(n => n.BlockMetadata.Ohlcv))
                .ToArray());

        _logger.LogInformation("Saving realized cap for {count:n0} block nodes.", blockNodes.Count);
        var updates = blockNodes.Values
            .Select(b => economicMappings.ToProperties(b))
            .ToList();

        await _graphDb.BulkUpdateNodePropertiesAsync(
            NodeKind.Block,
            nameof(BlockMetadata.Height),
            updates,
            ct);
    }
}
