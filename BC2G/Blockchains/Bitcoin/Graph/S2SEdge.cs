namespace BC2G.Blockchains.Bitcoin.Graph;

public class S2SEdge : Edge<ScriptNode, ScriptNode>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2S; }
    }

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

    public S2SEdge Update(double value)
    {
        return new S2SEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
