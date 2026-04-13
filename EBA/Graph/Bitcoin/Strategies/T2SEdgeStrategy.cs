using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class T2SEdgeStrategy(bool serializeCompressed)
    : BitcoinStrategyBase(
        $"edges_{T2SEdge.Kind.Source}_{T2SEdge.Kind.Relation}_{T2SEdge.Kind.Target}",
        serializeCompressed)
{
    public static readonly PropertyMapping<T2SEdge>[] Mappings = new MappingBuilder<T2SEdge>()
        .MapSourceId(TxNodeStrategy.IdSpace, e => e.Source.Txid)
        .MapTargetId(ScriptNodeStrategy.IdSpace, e => e.Target.Id)
        .MapValue(e => e.Value)
        .Map(e => e.Vout)
        .Map(e => e.CreationHeight)
        .Map(e => e.SpentHeight)
        .MapEdgeType(e => e.Relation)
        .ToArray();

    public override string GetCsvHeader()
    {
        return Mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsv((T2SEdge)edge);
    }

    public static string GetCsv(T2SEdge edge)
    {
        return Mappings.GetCsv(edge);
    }

    public static T2SEdge Deserialize(TxNode source, ScriptNode target, IRelationship relationship)
    {
        return new T2SEdge(
            source: source,
            target: target,
            timestamp: 0,
            creationHeight: Mappings.Get(nameof(T2SEdge.CreationHeight)).Deserialize<long>(relationship.Properties),
            value: Mappings.Get(Factory.ValueProperty.Name).Deserialize<long>(relationship.Properties),
            outputIndex: Mappings.Get(nameof(T2SEdge.Vout)).Deserialize<int>(relationship.Properties),
            spentHeight: Mappings.Get(nameof(T2SEdge.SpentHeight)).Deserialize<long>(relationship.Properties));
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
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