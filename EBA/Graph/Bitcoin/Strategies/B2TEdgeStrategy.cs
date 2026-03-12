using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class B2TEdgeStrategy(bool serializeCompressed) 
    : BitcoinStrategyBase(
        $"edges_{B2TEdge.Kind.Source}_{B2TEdge.Kind.Relation}_{B2TEdge.Kind.Target}",
        serializeCompressed)
{
    private static readonly PropertyMapping<B2TEdge>[] _mappings =
    [
        Factory.SourceId<B2TEdge>(BlockNodeStrategy.IdSpace, e => e.Source.BlockMetadata.Height),
        Factory.TargetId<B2TEdge>(TxNodeStrategy.IdSpace, e => e.Target.Txid),
        Factory.Value<B2TEdge>(e => e.Value),
        Factory.Height<B2TEdge>(e => e.BlockHeight),
        Factory.EdgeType<B2TEdge>(e => e.Relation)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsv((B2TEdge)edge);
    }

    public static string GetCsv(B2TEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public static B2TEdge Deserialize(BlockNode source, TxNode target, IRelationship relationship)
    {
        return new B2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            blockHeight: _mappings.Get(Factory.HeightProperty.Name).Deserialize<long>(relationship.Properties),
            value: _mappings.Get(Factory.ValueProperty.Name).Deserialize<long>(relationship.Properties));
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}