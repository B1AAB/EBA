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
        new(nameof(T2SEdge.Vout), FieldType.Int, e => e.Vout),
        new(nameof(T2SEdge.CreationHeight), FieldType.Long, e => e.CreationHeight),
        new(nameof(T2SEdge.SpentHeight), FieldType.Long, e => e.SpentHeight),
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
            creationHeight: _mappings.Get(nameof(T2SEdge.CreationHeight)).Deserialize<long>(relationship.Properties),
            value: _mappings.Get(Factory.ValueProperty.Name).Deserialize<long>(relationship.Properties),
            outputIndex: _mappings.Get(nameof(T2SEdge.Vout)).Deserialize<int>(relationship.Properties),
            spentHeight: _mappings.Get(nameof(T2SEdge.SpentHeight)).Deserialize<long>(relationship.Properties));
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}