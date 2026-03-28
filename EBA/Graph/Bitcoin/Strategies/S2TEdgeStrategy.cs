using EBA.Graph.Db.Neo4jDb;

using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class S2TEdgeStrategy(bool serializeCompressed)
    : BitcoinStrategyBase(
        $"edges_{S2TEdge.Kind.Source}_{S2TEdge.Kind.Relation}_{S2TEdge.Kind.Target}",
        serializeCompressed)
{
    private static readonly PropertyMapping<S2TEdge>[] _mappings =
    [
        Factory.SourceId<S2TEdge>(ScriptNodeStrategy.IdSpace, e => e.Source.Id),
        Factory.TargetId<S2TEdge>(TxNodeStrategy.IdSpace, e => e.Target.Txid),
        Factory.Value<S2TEdge>(e => e.Value),
        new(nameof(S2TEdge.SpentHeight), FieldType.Long, e => e.SpentHeight),
        new(nameof(S2TEdge.Txid), FieldType.String, e => e.Txid),
        new(nameof(S2TEdge.Vout), FieldType.Int, e => e.Vout),
        new(nameof(S2TEdge.Generated), FieldType.Boolean, e => e.Generated),
        Factory.EdgeType<S2TEdge>(e => e.Relation)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsvRow((S2TEdge)edge);
    }

    public static string GetCsvRow(S2TEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public static S2TEdge Deserialize(ScriptNode source, TxNode target, IRelationship relationship)
    {
        return new S2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            spentHeight: _mappings.Get(nameof(S2TEdge.SpentHeight)).Deserialize<long>(relationship.Properties),
            value: _mappings.Get(Factory.ValueProperty.Name).Deserialize<long>(relationship.Properties),
            txid: _mappings.Get(nameof(S2TEdge.Txid)).Deserialize<string>(relationship.Properties),
            vout: _mappings.Get(nameof(S2TEdge.Vout)).Deserialize<int>(relationship.Properties),
            generated: _mappings.Get(nameof(S2TEdge.Generated)).Deserialize<bool>(relationship.Properties)
        );
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}