namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2SEdge : Edge<TxNode, ScriptNode>
{
    public new static EdgeKind Kind => new(TxNode.Kind, ScriptNode.Kind, RelationType.Credits);

    public int Vout { get; }

    public long SpentInBlockHeight { get; }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long blockHeight,
        Output output,
        long spentInBlockHeight = long.MaxValue)
        : base(source, target, output.Value, Kind.Relation, timestamp, blockHeight)
    {
        Vout = output.N;
        SpentInBlockHeight = spentInBlockHeight;
    }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long blockHeight,
        long value,
        int outputIndex,
        long spentInBlockHeight = long.MaxValue) :
        base(source, target, value, Kind.Relation, timestamp, blockHeight)
    {
        Vout = outputIndex;
        SpentInBlockHeight = spentInBlockHeight;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Vout);
    }
}
