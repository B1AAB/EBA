namespace EBA.Blockchains.Bitcoin.GraphModel;

public class B2BEdge(
    BlockNode source,
    BlockNode target)
    : Edge<BlockNode, BlockNode>(
        source: source,
        target: target,
        value: 0,
        relation: Kind.Relation,
        timestamp: 0,
        height: 0)
{
    public static new EdgeKind Kind => new(BlockNode.Kind, BlockNode.Kind, RelationType.Follows);

    public new static string[] GetFeaturesName()
    {
        return [];
    }

    public override double[] GetFeatures()
    {
        return [];
    }
}
