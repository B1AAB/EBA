namespace EBA.Blockchains.Bitcoin.GraphModel;

public class C2TEdge(
    TxNode target,
    long value,
    uint timestamp,
    long blockHeight) : 
    T2TEdge(
        TxNode.GetCoinbaseNode(),
        target,
        value,
        RelationType.Mints,
        timestamp,
        blockHeight)
{
    public static EdgeKind Kind => new(CoinbaseNode.Kind, TxNode.Kind, RelationType.Mints);

    public C2TEdge Update(long value)
    {
        return new C2TEdge(Target, Value + value, Timestamp, BlockHeight);
    }
}
