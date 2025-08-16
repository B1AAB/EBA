namespace BC2G.Blockchains.Bitcoin.Graph;

public class S2BEdge : S2BEdge<ContextBase>
{
    public S2BEdge(
        ScriptNode<ContextBase> source,
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

public class S2BEdge<T> : Edge<ScriptNode<T>, BlockNode<T>, T>
    where T : IContext
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2B; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2B;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public S2BEdge(
        ScriptNode<T> source, BlockNode<T> target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.S2BRedeems;
    }

    public S2BEdge<T> Update(long value)
    {
        return new S2BEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
