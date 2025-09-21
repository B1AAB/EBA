namespace BC2G.Blockchains.Bitcoin.Graph;

public class B2TEdge : Edge<BlockNode, TxNode>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinB2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinB2T;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public B2TEdge(
        BlockNode source, TxNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.B2TConfirms;
    }

    public B2TEdge Update(long value)
    {
        return new B2TEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
