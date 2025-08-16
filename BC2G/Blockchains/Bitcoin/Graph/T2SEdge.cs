namespace BC2G.Blockchains.Bitcoin.Graph;

public class T2SEdge : T2SEdge<ContextBase>
{
    public T2SEdge(
        TxNode<ContextBase> source,
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

public class T2SEdge<T> : Edge<TxNode<T>, ScriptNode<T>, T>
    where T : IContext
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2S; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinT2S;
    }

    public EdgeLabel Label { get { return EdgeLabel.S2TTransfer; } }

    public T2SEdge(
        TxNode<T> source,
        ScriptNode<T> target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    { }

    public T2SEdge<T> Update(long value)
    {
        return new T2SEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
