namespace BC2G.Blockchains.Bitcoin.Graph;

public class BlockNode(Block block) :
    BlockNode<ContextBase>(block, new ContextBase()),
    IComparable<BlockNode<ContextBase>>,
    IEquatable<BlockNode<ContextBase>>
{ }

public class BlockNode<T> : Node<T>, IComparable<BlockNode<T>>, IEquatable<BlockNode<T>>
    where T : IContext
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinBlockNode; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return ComponentType;
    }

    public long Height { get; }
    public uint MedianTime { get; }
    public int TransactionsCount { get; }
    public double Difficulty { get; }
    public int Size { get; }
    public int StrippedSize { get; }
    public int Confirmations { get; }
    public int Weight { get; }

    public BlockNode(Block block, T context) : this(
        id: block.Height.ToString(),
        height: block.Height,
        medianTime: block.MedianTime,
        transactionsCount: block.TransactionsCount,
        difficulty: block.Difficulty,
        size: block.Size,
        strippedSize: block.StrippedSize,
        confirmations: block.Confirmations,
        weight: block.Weight,
        context: context)
    { }

    public BlockNode(
        string id,
        long height,
        uint medianTime,
        int transactionsCount,
        double difficulty,
        int size,
        int strippedSize,
        int confirmations,
        int weight,
        T context) : base(id, context)
    {
        Height = height;
        MedianTime = medianTime;
        TransactionsCount = transactionsCount;
        Difficulty = difficulty;
        Size = size;
        StrippedSize = strippedSize;
        Confirmations = confirmations;
        Weight = weight;
    }


    // TODO: all the following double-casting is because of the type
    // normalization happens when bulk-loading data into neo4j.
    // Find a better solution.

    public BlockNode(Neo4j.Driver.INode node, T context) :
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
            context: context)
    { }

    public override string GetUniqueLabel()
    {
        return Height.ToString();
    }

    public static new string[] GetFeaturesName()
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
            .. Node<T>.GetFeaturesName()
        ];
    }

    public override string[] GetFeatures()
    {
        return 
        [
            Height.ToString(), 
            MedianTime.ToString(), 
            TransactionsCount.ToString(), 
            Difficulty.ToString(), 
            Size.ToString(), 
            StrippedSize.ToString(), 
            Confirmations.ToString(), 
            Weight.ToString(),
            .. base.GetFeatures()
        ];
    }

    public int CompareTo(BlockNode<T>? other)
    {
        throw new NotImplementedException();
    }

    public bool Equals(BlockNode<T>? other)
    {
        throw new NotImplementedException();
    }
}
