namespace BC2G.Graph.Model;

public class S2SEdge : Edge<ScriptNode, ScriptNode>
{
    public S2SEdge(
        ScriptNode source, ScriptNode target,
        double value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    { }

    public S2SEdge(
        ScriptNode source, ScriptNode target,
        IRelationship relationship) :
        base(source, target, relationship)
    { }

    public static S2SEdge FromString(
        string[] fields, string sourceAddress, string targetAddress)
    {
        // TODO: fix creating node correctly.
        return new S2SEdge(
            source: new ScriptNode(fields[0], sourceAddress, ScriptType.Unknown),
            target: new ScriptNode(fields[1], targetAddress, ScriptType.Unknown),
            value: double.Parse(fields[2]),
            type: Enum.Parse<EdgeType>(fields[3]),
            timestamp: BitcoinAgent.GenesisTimestamp + uint.Parse(fields[4]),
            blockHeight: int.Parse(fields[5]));
    }
}
