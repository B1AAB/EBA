namespace BC2G.Blockchains.Bitcoin.Graph;

public class C2TEdge : T2TEdge
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinC2T; }
    }

    public C2TEdge(
        TxNode target, double value, uint timestamp, long blockHeight) :
        base(
            TxNode.GetCoinbaseNode(), target,
            value, EdgeType.Generation, timestamp, blockHeight)
    { }

    public new C2TEdge Update(double value)
    {
        return new C2TEdge(Target, Value + value, Timestamp, BlockHeight);
    }
}
