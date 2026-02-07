namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2NEdge(
    TxNode source,
    NullDataNode target,
    long value,
    EdgeType type,
    uint timestamp,
    long blockHeight) : 
    Edge<TxNode, NullDataNode>(
        source,
        target,
        value,
        type,
        EdgeLabel.T2Null,
        timestamp,
        blockHeight)
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2N; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinT2N;
    }
}
