namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2SEdge : Edge<TxNode, ScriptNode>
{
    public List<Output> Outputs { get; }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) :
        base(source, target, value, type, EdgeLabel.T2STransfer, timestamp, blockHeight)
    { }

    public T2SEdge(TxNode source,
        ScriptNode target,
        EdgeType type,
        uint timestamp,
        long blockHeight,
        List<Output> outputs) :
        base(source, target, outputs.Sum(x => x.Value), type, EdgeLabel.T2STransfer, timestamp, blockHeight)
    {
        Outputs = outputs;
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
            u.Type,
            u.Timestamp,
            u.BlockHeight,
            [.. u.Outputs, .. v.Outputs]);
    }
}
