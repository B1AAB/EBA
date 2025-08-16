namespace BC2G.Blockchains.Bitcoin.Graph;

public class S2TEdge : S2TEdge<ContextBase>
{
    public S2TEdge(
        ScriptNode<ContextBase> source,
        TxNode<ContextBase> target,
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

public class S2TEdge<T> : Edge<ScriptNode<T>, TxNode<T>, T>
    where T : IContext
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2T;
    }

    public EdgeLabel Label { get { return EdgeLabel.S2TTransfer; } }

    public S2TEdge(
        ScriptNode<T> source,
        TxNode<T> target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    { }

    public S2TEdge<T> Update(long value)
    {
        return new S2TEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
