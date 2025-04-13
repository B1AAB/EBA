namespace BC2G.Blockchains.Bitcoin.Graph;

public class S2SEdge : Edge<ScriptNode, ScriptNode>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2S; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2S;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public S2SEdge(
        ScriptNode source, ScriptNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = Type == EdgeType.Transfers ? EdgeLabel.S2STransfer : EdgeLabel.S2SFee;
    }

    /*
    public S2SEdge(
        ScriptNode source, ScriptNode target,
        IRelationship relationship) :
        base(source, target, relationship)
    {
        _label = Type == EdgeType.Transfers ? EdgeLabel.S2STransfer : EdgeLabel.S2SFee;
    }*/

    public S2SEdge Update(long value)
    {
        return new S2SEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
