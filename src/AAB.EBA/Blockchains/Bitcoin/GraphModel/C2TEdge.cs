namespace AAB.EBA.Blockchains.Bitcoin.GraphModel;

public class C2TEdge(
    TxNode target,
    long value,
    long height)
    : Edge<CoinbaseNode, TxNode>(
        source: new CoinbaseNode(),
        target: target,
        value: value,
        relation: RelationType.Mints,
        height: height)
{
    public new static EdgeKind Kind => new(CoinbaseNode.Kind, TxNode.Kind, RelationType.Mints);

    public C2TEdge Update(long value)
    {
        return new C2TEdge(Target, Value + value, Height);
    }
}
