namespace BC2G.Blockchains.Bitcoin.Graph;


// TODO: this class seems redundant given Bitcoin.Model.Block
// can these two, and other similar classes merged?

public class BlockNode(
    string id,
    long height,
    uint medianTime,
    int transactionsCount,
    double difficulty,
    int size,
    int strippedSize,
    int confirmations,
    int weight, 
    double? originalIndegree = null,
    double? originalOutdegree = null) : 
    Node(id, originalIndegree: originalIndegree, originalOutdegree: originalOutdegree), 
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

    public long Height { get; } = height;
    public uint MedianTime { get; } = medianTime;
    public int TransactionsCount { get; } = transactionsCount;
    public double Difficulty { get; } = difficulty;
    public int Size { get; } = size;
    public int StrippedSize { get; } = strippedSize;
    public int Confirmations { get; } = confirmations;
    public int Weight { get; } = weight;


    // TODO: all the following double-casting is because of the type
    // normalization happens when bulk-loading data into neo4j.
    // Find a better solution.

    public BlockNode(Neo4j.Driver.INode node, double? originalIndegree = null, double? originalOutdegree = null) :
        this(
            id: node.ElementId,
            height: long.Parse((string)node.Properties[Props.Height.Name]),
            medianTime: (uint)(long)node.Properties[Props.BlockMedianTime.Name],
            transactionsCount: (int)(long)node.Properties[Props.BlockTxCount.Name],
            difficulty: (double)node.Properties[Props.BlockDifficulty.Name],
            size: (int)(long)node.Properties[Props.BlockSize.Name],
            strippedSize: (int)(long)node.Properties[Props.BlockStrippedSize.Name],
            confirmations: (int)(long)node.Properties[Props.BlockConfirmations.Name],
            weight: (int)(long)node.Properties[Props.BlockWeight.Name],
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree)
    { }

    public override string GetUniqueLabel()
    {
        return Height.ToString();
    }

    public new static string[] GetFeaturesName()
    {
        return 
        [
            nameof(Height),
            nameof(MedianTime),
            nameof(TransactionsCount),
            nameof(Difficulty),
            nameof(Size),
            nameof(StrippedSize),
            nameof(Confirmations),
            nameof(Weight),
            .. Node.GetFeaturesName()
        ];
    }

    public override double[] GetFeatures()
    {
        return 
        [
            Height, 
            MedianTime, 
            TransactionsCount, 
            Difficulty, 
            Size, 
            StrippedSize, 
            Confirmations, 
            Weight,
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
