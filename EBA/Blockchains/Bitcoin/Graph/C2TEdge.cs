namespace BC2G.Blockchains.Bitcoin.Graph;

public class C2TEdge : T2TEdge
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
        TxNode target, long value, uint timestamp, long blockHeight) :
        base(
            TxNode.GetCoinbaseNode(), target,
            value, EdgeType.Mints, timestamp, blockHeight)
    { }

    public new C2TEdge Update(long value)
    {
        return new C2TEdge(Target, Value + value, Timestamp, BlockHeight);
    }
}
