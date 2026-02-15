namespace EBA.Blockchains.Bitcoin.GraphModel;

public class B2TEdge : Edge<BlockNode, TxNode>
{
    public B2TEdge(
        BlockNode source, TxNode target,
        long value, EdgeType type,
        uint timestamp, long blockHeight) :
        base(source, target, value, type, EdgeLabel.B2TConfirms, timestamp, blockHeight)
    { }

    public B2TEdge Update(long value)
    {
        return new B2TEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
