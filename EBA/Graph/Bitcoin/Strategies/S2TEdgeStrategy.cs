namespace EBA.Graph.Bitcoin.Strategies;

public class S2TEdgeStrategy(bool serializeCompressed)
    : StrategyBase<S2TEdge, S2TEdgeStrategy>(
        $"edges_{S2TEdge.Kind.Source}_{S2TEdge.Kind.Relation}_{S2TEdge.Kind.Target}",
        serializeCompressed),
    IElementSchema<S2TEdge>
{
    public static EntityTypeMapper<S2TEdge> Mapper { get; } = new EntityTypeMapper<S2TEdge>(
        new MappingBuilder<S2TEdge>()
            .MapSourceId(ScriptNodeStrategy.IdSpace, e => e.Source.Id)
            .MapTargetId(TxNodeStrategy.IdSpace, e => e.Target.Txid)
            .MapValue(e => e.Value)
            .Map(e => e.SpentHeight)
            .Map(e => e.Txid)
            .Map(e => e.Vout)
            .Map(e => e.Generated)
            .Map(e => e.CreationHeight)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static S2TEdge Deserialize(ScriptNode source, TxNode target, IRelationship relationship)
    {
        return new S2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            creationHeight: Mapper.GetValue(x => x.CreationHeight, relationship.Properties),
            spentHeight: Mapper.GetValue(x => x.SpentHeight, relationship.Properties),
            value: Mapper.GetValue(x => x.Value, relationship.Properties),
            txid: Mapper.GetValue(x => x.Txid, relationship.Properties),
            vout: Mapper.GetValue(x => x.Vout, relationship.Properties),
            generated: Mapper.GetValue(x => x.Generated, relationship.Properties)
        );
    }
}