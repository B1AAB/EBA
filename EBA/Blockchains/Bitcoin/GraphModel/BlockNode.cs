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
    public BlockMetadata BlockMetadata { init; get; } = blockMetadata;

    public uint[] EdgeLabelCount { set; get; } = [];
    public long[] EdgeLabelValueSum { set; get; } = [];

    public double ResidualValue
    {
        get
        {
            if (BlockMetadata.InputValues is null || BlockMetadata.OutputValues is null)
            {
                return 0;
            }

            return BlockMetadata.InputValues.Sum - BlockMetadata.OutputValues.Sum - BlockMetadata.Fees.Sum;
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
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.InputCounts)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.OutputCounts)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.InputValues)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.OutputValues)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.SpentOutputAge)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.Fees)),
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
            .. BlockMetadata.InputCounts.GetFeatures(),
            .. BlockMetadata.OutputCounts.GetFeatures(),
            .. BlockMetadata.InputValues.GetFeatures(),
            .. BlockMetadata.OutputValues.GetFeatures(),
            .. BlockMetadata.SpentOutputAge.GetFeatures(),
            .. BlockMetadata.Fees.GetFeatures(),
            .. base.GetFeatures()
        ];
    }

    public int CompareTo(BlockNode? other)
    {
        throw new NotImplementedException();
    }

    public bool Equals(BlockNode? other)
    {
        throw new NotImplementedException();
    }
}
