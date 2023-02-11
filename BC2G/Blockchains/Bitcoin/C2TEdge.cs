namespace BC2G.Blockchains.Bitcoin;

public class C2TEdge : T2TEdge
{
    public C2TEdge(
        TxNode target, double value, uint timestamp, long blockHeight) :
        base(
            TxNode.GetCoinbaseNode(), target,
            value, EdgeType.Generation, timestamp, blockHeight)
    { }
}
