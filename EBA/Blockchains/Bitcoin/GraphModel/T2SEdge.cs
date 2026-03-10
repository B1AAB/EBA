namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2SEdge : Edge<TxNode, ScriptNode>
{
    public new static EdgeKind Kind => new(TxNode.Kind, ScriptNode.Kind, RelationType.Credits);

    public List<long> TxOValues { get; }
    public int TxOCount { get { return TxOValues.Count; } }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long blockHeight,
        List<Output> outputs) :
        this(source, target, timestamp, blockHeight, outputs.Select(x => x.Value).ToList())
    { }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        uint timestamp,
        long blockHeight,
        List<long> values) :
        base(source, target, values.Sum(), Kind.Relation, timestamp, blockHeight)
    {
        TxOValues = values;
    }

    /// <summary>
    /// Use this method when you're sure the two edges are identical 
    /// (e.g., same source and destination) to merge only their outputs list 
    /// and sum of value.
    /// </summary>
    public static T2SEdge Merge(T2SEdge u, T2SEdge v)
    {
        return new T2SEdge(
            u.Source,
            u.Target,
            u.Timestamp,
            u.BlockHeight,
            [.. u.TxOValues, .. v.TxOValues]);
    }
}
