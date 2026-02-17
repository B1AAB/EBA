namespace EBA.Blockchains.Bitcoin.GraphModel;

public class B2TEdge : Edge<BlockNode, TxNode>
{
    public B2TEdge( // TODO: this does not need relation it is alwasy contains
        BlockNode source, TxNode target,
        long value, RelationType relation,
        uint timestamp, long blockHeight) :
        base(source, target, value, relation, timestamp, blockHeight)
    { }

    public new static EdgeKind Kind => new(BlockNode.Kind, TxNode.Kind, RelationType.Contains);

    public B2TEdge Update(long value)
    {
        return new B2TEdge(Source, Target, Value + value, Relation, Timestamp, BlockHeight);
    }
}
