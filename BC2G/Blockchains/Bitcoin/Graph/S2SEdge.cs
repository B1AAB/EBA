namespace BC2G.Blockchains.Bitcoin.Graph;

public class S2SEdge : S2SEdge<ContextBase>
{
    public S2SEdge(
        ScriptNode<ContextBase> source,
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

public class S2SEdge<T> : Edge<ScriptNode<T>, ScriptNode<T>, T>
    where T : IContext
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2S; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2S;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public S2SEdge(
        ScriptNode<T> source, ScriptNode<T> target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = Type == EdgeType.Transfers ? EdgeLabel.S2STransfer : EdgeLabel.S2SFee;
    }

    public S2SEdge<T> Update(long value)
    {
        return new S2SEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
