namespace BC2G.Blockchains.Bitcoin.Graph;

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

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public S2BEdge(
        ScriptNode source, BlockNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.S2BRedeems;
    }

    public S2BEdge Update(long value)
    {
        return new S2BEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
