namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2OEdge : Edge<TxNode, NonStandardScriptNode>
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2O; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinT2O;
    }

    public T2OEdge(
        TxNode source,
        NonStandardScriptNode target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) :
        base(source, target, value, type, EdgeLabel.T2NonStandard, timestamp, blockHeight)
    { }
}