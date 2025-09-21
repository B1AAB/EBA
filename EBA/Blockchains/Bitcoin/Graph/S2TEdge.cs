namespace EBA.Blockchains.Bitcoin.Graph;

public class S2TEdge : Edge<ScriptNode, TxNode>
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2T;
    }

    public EdgeLabel Label { get { return EdgeLabel.S2TTransfer; } }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    { }

    public S2TEdge Update(long value)
    {
        return new S2TEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
