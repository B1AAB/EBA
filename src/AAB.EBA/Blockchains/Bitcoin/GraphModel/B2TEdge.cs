namespace AAB.EBA.Blockchains.Bitcoin.GraphModel;

public class B2TEdge : Edge<BlockNode, TxNode>
{
    public new static EdgeKind Kind => new(BlockNode.Kind, TxNode.Kind, RelationType.Confirms);

    public B2TEdge(
        BlockNode source, TxNode target,
        long value, 
        long height) :
        base(
            source: source,
            target: target,
            value: value,
            relation: Kind.Relation,
            height: height)
    { }    

    public B2TEdge Update(long value)
    {
        return new B2TEdge(Source, Target, Value + value, Height);
    }
}
