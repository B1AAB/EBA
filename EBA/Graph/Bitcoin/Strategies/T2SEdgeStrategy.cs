using EBA.Graph.Db.Neo4jDb;

using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class T2SEdgeStrategy(bool serializeCompressed) 
    : BitcoinStrategyBase(
        $"edges_{T2SEdge.Kind.Source}_{T2SEdge.Kind.Relation}_{T2SEdge.Kind.Target}",
        serializeCompressed)
{
    public static readonly PropertyMapping<T2SEdge>[] _mappings =
    [
        Factory.SourceId<T2SEdge>(TxNodeStrategy.IdSpace, e => e.Source.Txid),
        Factory.TargetId<T2SEdge>(ScriptNodeStrategy.IdSpace, e => e.Target.Id),
        Factory.Value<T2SEdge>(e => e.Value),
        new(nameof(T2SEdge.TxOValues), 
            FieldType.LongArray,
            e => e.TxOValues,
            deserializer: v => 
            
            ((IList<object>)v!).Select(x => (long)x).ToArray()), // TODO: check if this can be simplified, not sure if you need that additional casting, maybe you can just let the base deserializer do its job
        new(nameof(T2SEdge.TxOCount), FieldType.Long, n => n.TxOCount),
        Factory.Height<T2SEdge>(e => e.BlockHeight),
        Factory.EdgeType<T2SEdge>(e => e.Relation),
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsv((T2SEdge)edge);
    }

    public static string GetCsv(T2SEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public static T2SEdge Deserialize(TxNode source, ScriptNode target, IRelationship relationship)
    {
        return new T2SEdge(
            source: source,
            target: target,
            timestamp: 0,
            blockHeight: _mappings.Get(Factory.HeightProperty.Name).Deserialize<long>(relationship.Properties),
            values: [.. _mappings.Get(nameof(T2SEdge.TxOValues)).Deserialize<long[]>(relationship.Properties) ?? []]);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}