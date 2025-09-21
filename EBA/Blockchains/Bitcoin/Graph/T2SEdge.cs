namespace EBA.Blockchains.Bitcoin.Graph;

public class T2SEdge : Edge<TxNode, ScriptNode>
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2S; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinT2S;
    }

    public EdgeLabel Label { get { return EdgeLabel.S2TTransfer; } }

    public T2SEdge(
        TxNode source,
        ScriptNode target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    { }

    public T2SEdge Update(long value)
    {
        return new T2SEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
