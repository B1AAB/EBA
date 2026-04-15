namespace EBA.Graph.Bitcoin.Strategies;

public class T2SEdgeStrategy(bool serializeCompressed)
    : StrategyBase<T2SEdge, T2SEdgeStrategy>(
        $"edges_{T2SEdge.Kind.Source}_{T2SEdge.Kind.Relation}_{T2SEdge.Kind.Target}",
        serializeCompressed),
    IElementSchema<T2SEdge>
{
    public static EntityTypeMapper<T2SEdge> Mapper { get; } = new EntityTypeMapper<T2SEdge>(
        new MappingBuilder<T2SEdge>()
            .MapSourceId(TxNodeStrategy.IdSpace, e => e.Source.Txid)
            .MapTargetId(ScriptNodeStrategy.IdSpace, e => e.Target.Id)
            .MapValue(e => e.Value)
            .Map(e => e.Vout)
            .Map(e => e.CreationHeight)
            .Map(e => e.SpentHeight)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static T2SEdge Deserialize(TxNode source, ScriptNode target, IRelationship relationship)
    {
        return new T2SEdge(
            source: source,
            target: target,
            timestamp: 0,
            creationHeight: Mapper.GetValue(x=>x.CreationHeight, relationship.Properties),
            value: Mapper.GetValue(x => x.Value, relationship.Properties),
            outputIndex: Mapper.GetValue(x => x.Vout, relationship.Properties),
            spentHeight: Mapper.GetValue(x => x.SpentHeight, relationship.Properties));
    }

    public override string[] GetSchemaConfigs()
    {
        return 
        [
            $"CREATE INDEX utxo_spending_idx IF NOT EXISTS " +
            $"FOR ()-[r:{T2SEdge.Kind.Relation}]-() " +
            $"ON (r.{nameof(T2SEdge.CreationHeight)}, r.{nameof(T2SEdge.SpentHeight)})"
        ];
    }
}