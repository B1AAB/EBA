namespace EBA.Graph.Bitcoin.Strategies;

public class B2TEdgeStrategy(bool serializeCompressed)
    : StrategyBase<B2TEdge, B2TEdgeStrategy>(
        $"edges_{B2TEdge.Kind.Source}_{B2TEdge.Kind.Relation}_{B2TEdge.Kind.Target}",
        serializeCompressed),
    IElementSchema<B2TEdge>
{
    public static EntityTypeMapper<B2TEdge> Mapper { get; } = new EntityTypeMapper<B2TEdge>(
        new MappingBuilder<B2TEdge>()
            .MapSourceId(BlockNodeStrategy.IdSpace, e => e.Source.BlockMetadata.Height)
            .MapTargetId(TxNodeStrategy.IdSpace, e => e.Target.Txid)
            .MapValue(e => e.Value)
            .MapBlockHeight(e => e.BlockHeight)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static B2TEdge Deserialize(BlockNode source, TxNode target, IRelationship relationship)
    {
        return new B2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            blockHeight: Mapper.GetValue(x => x.BlockHeight, relationship.Properties),
            value: Mapper.GetValue(x => x.Value, relationship.Properties));
    }
}