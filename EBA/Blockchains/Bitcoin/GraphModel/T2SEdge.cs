namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2SEdge : Edge<TxNode, ScriptNode>
{
    public new static EdgeKind Kind => new(TxNode.Kind, ScriptNode.Kind, RelationType.Credits);

    public int OutputIndex { get; }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long blockHeight,
        Output output)
        : base(source, target, output.Value, Kind.Relation, timestamp, blockHeight)
    {
        OutputIndex = output.Index;
    }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long blockHeight,
        long value,
        int outputIndex) :
        base(source, target, value, Kind.Relation, timestamp, blockHeight)
    {
        OutputIndex = outputIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), OutputIndex);
    }
}
