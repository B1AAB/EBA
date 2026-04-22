using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class BlockNode(
    BlockMetadata blockMetadata,
    double? originalIndegree = null,
    double? originalOutdegree = null,
    double? outHopsFromRoot = null,
    string? idInGraphDb = null) : 
    Node(
        id: blockMetadata.Height.ToString(),
        originalInDegree: originalIndegree,
        originalOutDegree: originalOutdegree,
        outHopsFromRoot: outHopsFromRoot,
        idInGraphDb: idInGraphDb), 
    IComparable<BlockNode>, IEquatable<BlockNode>
{
    public new static NodeKind Kind => NodeKind.Block;
    public override NodeKind NodeKind => Kind;

    public BlockMetadata BlockMetadata { init; get; } = blockMetadata;

    public Dictionary<EdgeKind, uint> TripletTypeCount { set; get; } = [];
    public Dictionary<EdgeKind, long> TripletTypeValueSum { set; get; } = [];

    public double ResidualValue
    {
        get
        {
            if (BlockMetadata.InputValuesStats is null || BlockMetadata.OutputValuesStats is null)
            {
                return 0;
            }

            return BlockMetadata.InputValuesStats.Sum - BlockMetadata.OutputValuesStats.Sum - BlockMetadata.FeesStats.Sum;
        }
    }

    public BlockNode(Block block) : this(blockMetadata: block) { }

    public override string GetIdPropertyName()
    {
        return nameof(BlockMetadata.Height);
    }

    public new static string[] GetFeaturesName()
    {
        return 
        [
            nameof(BlockMetadata.Height),
            nameof(BlockMetadata.MedianTime),
            nameof(BlockMetadata.TransactionsCount),
            nameof(BlockMetadata.Difficulty),
            nameof(BlockMetadata.Size),
            nameof(BlockMetadata.StrippedSize),
            nameof(BlockMetadata.Weight),
            nameof(BlockMetadata.CoinbaseOutputsCount),
            nameof(BlockMetadata.MintedBitcoins),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.InputCountsStats)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.OutputCountsStats)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.InputValuesStats)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.OutputValuesStats)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.SpentOutputAgeStats)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.FeesStats)),
            .. Node.GetFeaturesName()
        ];
    }

    public override string[] GetFeatures()
    {
        return 
        [
            BlockMetadata.Height.ToString(), 
            BlockMetadata.MedianTime.ToString(), 
            BlockMetadata.TransactionsCount.ToString(), 
            BlockMetadata.Difficulty.ToString(), 
            BlockMetadata.Size.ToString(), 
            BlockMetadata.StrippedSize.ToString(),
            BlockMetadata.Weight.ToString(),
            BlockMetadata.CoinbaseOutputsCount.ToString(),
            BlockMetadata.MintedBitcoins.ToString(),
            .. BlockMetadata.InputCountsStats.GetFeatures(),
            .. BlockMetadata.OutputCountsStats.GetFeatures(),
            .. BlockMetadata.InputValuesStats.GetFeatures(),
            .. BlockMetadata.OutputValuesStats.GetFeatures(),
            .. BlockMetadata.SpentOutputAgeStats.GetFeatures(),
            .. BlockMetadata.FeesStats.GetFeatures(),
            .. base.GetFeatures()
        ];
    }

    public int CompareTo(BlockNode? other)
    {
        throw new NotImplementedException("BlockNode.CompareTo is not implemented.");
    }

    public bool Equals(BlockNode? other)
    {
        throw new NotImplementedException("BlockNode.Equals is not implemented.");
    }
}
