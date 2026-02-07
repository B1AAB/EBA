namespace EBA.Blockchains.Bitcoin.GraphModel;

public class S2BEdge : Edge<ScriptNode, BlockNode>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2B; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2B;
    }

    public S2BEdge(
        ScriptNode source, BlockNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, EdgeLabel.S2BRedeems, timestamp, blockHeight)
    { }
}
