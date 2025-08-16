namespace BC2G.Blockchains.Bitcoin.Graph;

public class B2TEdge : B2TEdge<ContextBase>
{
    public B2TEdge(BlockNode<ContextBase> source,
        TxNode<ContextBase> target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) : base(source,
            target,
            value,
            type,
            timestamp,
            blockHeight)
    { }
}

public class B2TEdge<T> : Edge<BlockNode<T>, TxNode<T>, T>
    where T : IContext
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinB2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinB2T;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public B2TEdge(
        BlockNode<T> source, TxNode<T> target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.B2TConfirms;
    }

    public B2TEdge<T> Update(long value)
    {
        return new B2TEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
