namespace BC2G.Blockchains.Bitcoin.Graph;

public class T2BEdge : T2BEdge<ContextBase>
{
    public T2BEdge(TxNode<ContextBase> source,
        BlockNode<ContextBase> target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) : base(
            source,
            target,
            value,
            type,
            timestamp,
            blockHeight)
    { }
}

public class T2BEdge<T> : Edge<TxNode<T>, BlockNode<T>, T>
    where T : IContext
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2B; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinT2B;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public T2BEdge(
        TxNode<T> source, BlockNode<T> target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.T2BRedeems;
    }

    public T2BEdge<T> Update(long value)
    {
        return new T2BEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
