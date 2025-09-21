namespace BC2G.Blockchains.Bitcoin.Graph;

public class T2BEdge : Edge<TxNode, BlockNode>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2B; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinT2B;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public T2BEdge(
        TxNode source, BlockNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = EdgeLabel.T2BRedeems;
    }

    public T2BEdge Update(long value)
    {
        return new T2BEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
