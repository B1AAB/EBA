namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2BEdge : Edge<TxNode, BlockNode>
{
    public T2BEdge(
        TxNode source, BlockNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, EdgeLabel.T2BRedeems, timestamp, blockHeight)
    { }
}
