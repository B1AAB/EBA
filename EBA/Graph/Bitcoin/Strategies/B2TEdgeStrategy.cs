using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class B2TEdgeStrategy(bool serializeCompressed) 
    : BitcoinStrategyBase(
        $"edges_{B2TEdge.Kind.Source}_{B2TEdge.Kind.Relation}_{B2TEdge.Kind.Target}",
        serializeCompressed)
{
    public static readonly PropertyMapping<B2TEdge>[] Mappings = new MappingBuilder<B2TEdge>()
        .MapSourceId(BlockNodeStrategy.IdSpace, e => e.Source.BlockMetadata.Height)
        .MapTargetId(TxNodeStrategy.IdSpace, e => e.Target.Txid)
        .MapValue(e => e.Value)
        .MapBlockHeight(e => e.BlockHeight)
        .MapEdgeType(e => e.Relation)
        .ToArray();

    public override string GetCsvHeader()
    {
        return Mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsv((B2TEdge)edge);
    }

    public static string GetCsv(B2TEdge edge)
    {
        return Mappings.GetCsv(edge);
    }

    public static B2TEdge Deserialize(BlockNode source, TxNode target, IRelationship relationship)
    {
        return new B2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            blockHeight: Mappings.Get(Factory.HeightProperty.Name).Deserialize<long>(relationship.Properties),
            value: Mappings.Get(Factory.ValueProperty.Name).Deserialize<long>(relationship.Properties));
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}