using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class B2BEdgeStrategy(bool serializeCompressed)
     : BitcoinStrategyBase(
         $"edges_{B2BEdge.Kind.Source}_{B2BEdge.Kind.Relation}_{B2BEdge.Kind.Target}",
         serializeCompressed)
{
    private static readonly PropertyMapping<B2BEdge>[] _mappings =
    [
        Factory.SourceId<B2BEdge>(BlockNodeStrategy.IdSpace, e => e.BlockHeight),
        Factory.TargetId<B2BEdge>(BlockNodeStrategy.IdSpace, e => e.BlockHeight),
        Factory.EdgeType<B2BEdge>(e => e.Relation)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsvRow((B2BEdge)edge);
    }

    public static string GetCsvRow(B2BEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public static B2BEdge Deserialize(BlockNode source, BlockNode target, IRelationship relationship)
    {
        return new B2BEdge(source: source, target: target);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
