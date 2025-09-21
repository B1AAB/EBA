namespace BC2G.Blockchains.Bitcoin.Graph;

public class B2SEdge : Edge<BlockNode, ScriptNode>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinB2S; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinB2S;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public B2SEdge(
        BlockNode source, ScriptNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.B2SCredits;
    }

    public B2SEdge Update(long value)
    {
        return new B2SEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
