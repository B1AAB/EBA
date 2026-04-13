using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class S2TEdgeStrategy(bool serializeCompressed)
    : BitcoinStrategyBase(
        $"edges_{S2TEdge.Kind.Source}_{S2TEdge.Kind.Relation}_{S2TEdge.Kind.Target}",
        serializeCompressed)
{
    public static readonly PropertyMapping<S2TEdge>[] Mappings = new MappingBuilder<S2TEdge>()
        .MapSourceId(ScriptNodeStrategy.IdSpace, e => e.Source.Id)
        .MapTargetId(TxNodeStrategy.IdSpace, e => e.Target.Txid)
        .MapValue(e => e.Value)
        .Map(e => e.SpentHeight)
        .Map(e => e.Txid)
        .Map(e => e.Vout)
        .Map(e => e.Generated)
        .Map(e => e.CreationHeight)
        .MapEdgeType(e => e.Relation)
        .ToArray();

    public override string GetCsvHeader()
    {
        return Mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsvRow((S2TEdge)edge);
    }

    public static string GetCsvRow(S2TEdge edge)
    {
        return Mappings.GetCsv(edge);
    }

    public static S2TEdge Deserialize(ScriptNode source, TxNode target, IRelationship relationship)
    {
        return new S2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            creationHeight: Mappings.Get(nameof(S2TEdge.CreationHeight)).Deserialize<long>(relationship.Properties),
            spentHeight: Mappings.Get(nameof(S2TEdge.SpentHeight)).Deserialize<long>(relationship.Properties),
            value: Mappings.Get(Factory.ValueProperty.Name).Deserialize<long>(relationship.Properties),
            txid: Mappings.Get(nameof(S2TEdge.Txid)).Deserialize<string>(relationship.Properties),
            vout: Mappings.Get(nameof(S2TEdge.Vout)).Deserialize<int>(relationship.Properties),
            generated: Mappings.Get(nameof(S2TEdge.Generated)).Deserialize<bool>(relationship.Properties)
        );
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}