namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2SEdge : Edge<TxNode, ScriptNode>
{
    public new static EdgeKind Kind => new(TxNode.Kind, ScriptNode.Kind, RelationType.Credits);

    public int Vout { get; }

    public long SpentHeight { get; }

    public decimal? FiatValue { get; } = null;

    public long CreationHeight => BlockHeight;

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long creationHeight,
        Output output,
        decimal? fiatValue = null,
        long spentHeight = long.MaxValue)
        : base(
            source: source,
            target: target,
            relation: Kind.Relation,
            value: output.Value,
            timestamp: timestamp,
            blockHeight: creationHeight)
    {
        Vout = output.N;
        SpentHeight = spentHeight;
        FiatValue = fiatValue;
    }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long creationHeight,
        long value,
        int outputIndex,
        long spentHeight = long.MaxValue) :
        base(
            source: source, 
            target: target,
            relation: Kind.Relation,
            value: value, 
            timestamp: timestamp, 
            blockHeight: creationHeight)
    {
        Vout = outputIndex;
        SpentHeight = spentHeight;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Vout);
    }
}
