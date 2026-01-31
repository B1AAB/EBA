using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;
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
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinBlockNode; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return ComponentType;
    }

    public BlockMetadata BlockMetadata { init; get; } = blockMetadata;

    public uint[] EdgeLabelCount { set; get; } = [];
    public long[] EdgeLabelValueSum { set; get; } = [];

    public BlockNode(Block block) : this(blockMetadata: block) { }


    // TODO: all the following double-casting is because of the type
    // normalization happens when bulk-loading data into neo4j.
    // Find a better solution.

    public BlockNode(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double outHopsFromRoot) :
        this(
            blockMetadata: BlockNodeStrategy.GetNodeFromProps(node.Properties),
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            outHopsFromRoot: outHopsFromRoot,
            idInGraphDb: node.ElementId)
    { }

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
            nameof(BlockMetadata.TxFees),
            nameof(BlockMetadata.MintedBitcoins),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.InputCounts)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.OutputCounts)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.InputValues)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.OutputValues)),
            .. DescriptiveStatistics.GetFeaturesName(nameof(BlockMetadata.SpentOutputAge)),
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
            BlockMetadata.TxFees.ToString(),
            BlockMetadata.MintedBitcoins.ToString(),
            .. BlockMetadata.InputCounts.GetFeatures(),
            .. BlockMetadata.OutputCounts.GetFeatures(),
            .. BlockMetadata.InputValues.GetFeatures(),
            .. BlockMetadata.OutputValues.GetFeatures(),
            .. BlockMetadata.SpentOutputAge.GetFeatures(),
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
