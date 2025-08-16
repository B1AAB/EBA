namespace BC2G.Blockchains.Bitcoin.Graph;

public class B2SEdge : B2SEdge<ContextBase>
{
    public B2SEdge(
        BlockNode<ContextBase> source,
        ScriptNode<ContextBase> target,
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

public class B2SEdge<T> : Edge<BlockNode<T>, ScriptNode<T>, T>
    where T : IContext
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinB2S; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinB2S;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public B2SEdge(
        BlockNode<T> source, ScriptNode<T> target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.B2SCredits;
    }

    public B2SEdge<T> Update(long value)
    {
        return new B2SEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
