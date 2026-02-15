namespace EBA.Blockchains.Bitcoin.GraphModel;

public class C2TEdge : T2TEdge
{
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
