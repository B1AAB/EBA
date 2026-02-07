namespace EBA.Blockchains.Bitcoin.GraphModel;

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

    public S2SEdge(
        ScriptNode source, ScriptNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, type == EdgeType.Transfers ? EdgeLabel.S2STransfer : EdgeLabel.S2SFee, timestamp, blockHeight)
    { }
}
