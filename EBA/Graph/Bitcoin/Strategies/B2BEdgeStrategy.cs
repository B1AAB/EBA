using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class B2BEdgeStrategy(bool serializeCompressed)
     : BitcoinStrategyBase(
         $"edges_{B2BEdge.Kind.Source}_{B2BEdge.Kind.Relation}_{B2BEdge.Kind.Target}",
         serializeCompressed)
{
    public static readonly PropertyMapping<B2BEdge>[] Mappings =
    [
        Factory.SourceId<B2BEdge>(BlockNodeStrategy.IdSpace, e => e.BlockHeight),
        Factory.TargetId<B2BEdge>(BlockNodeStrategy.IdSpace, e => e.BlockHeight),
        Factory.EdgeType<B2BEdge>(e => e.Relation)
    ];

    public override string GetCsvHeader()
    {
        return Mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsvRow((B2BEdge)edge);
    }

    public static string GetCsvRow(B2BEdge edge)
    {
        return Mappings.GetCsv(edge);
    }

    public static B2BEdge Deserialize(BlockNode source, BlockNode target, IRelationship relationship)
    {
        return new B2BEdge(source: source, target: target);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }

    public override string[] GetSeedingCommands()
    {
        return
        [
            $"MATCH (target:Block), (source:Block) " +
            $"WHERE target.{nameof(B2BEdge.BlockHeight)} + 1 = source.{nameof(B2BEdge.BlockHeight)} " +
            $"MERGE (target)-[:{RelationType.Follows}]->(source)"
        ];
    }
}
