namespace EBA.Blockchains.Bitcoin.GraphModel;

public class B2BEdge(
    BlockNode source,
    BlockNode target)
    : Edge<BlockNode, BlockNode>(source, target, 0, Kind.Relation, 0, 0)
{
    public static new EdgeKind Kind => new(BlockNode.Kind, BlockNode.Kind, RelationType.Follows);
}
