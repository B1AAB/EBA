namespace BC2G.Blockchains.Bitcoin.Graph;

public class C2TEdge : C2TEdge<ContextBase>
{
    public C2TEdge(
        TxNode<ContextBase> target,
        long value,
        uint timestamp,
        long blockHeight) : base(
            target,
            value,
            timestamp,
            blockHeight)
    { }
}

public class C2TEdge<T> : T2TEdge<T>
    where T : IContext
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinC2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinC2T;
    }

    public new EdgeLabel Label { get; } = EdgeLabel.C2TMinting;

    public C2TEdge(
        TxNode<T> target, long value, uint timestamp, long blockHeight) :
        base(
            TxNode<T>.GetCoinbaseNode(), target,
            value, EdgeType.Mints, timestamp, blockHeight)
    { }

    public new C2TEdge<T> Update(long value)
    {
        return new C2TEdge<T>(Target, Value + value, Timestamp, BlockHeight);
    }
}
