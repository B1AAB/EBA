namespace EBA.Graph.Bitcoin.Strategies;

public class B2BEdgeStrategy(bool serializeCompressed) 
    : StrategyBase<B2BEdge, B2BEdgeStrategy>(
        $"edges_{B2BEdge.Kind.Source}_{B2BEdge.Kind.Relation}_{B2BEdge.Kind.Target}",
        serializeCompressed), 
    IElementSchema<B2BEdge>
{
    public static EntityTypeMapper<B2BEdge> Mapper { get; } = new EntityTypeMapper<B2BEdge>(
        new MappingBuilder<B2BEdge>()
            .MapSourceId(BlockNodeStrategy.IdSpace, e => e.BlockHeight)
            .MapTargetId(BlockNodeStrategy.IdSpace, e => e.BlockHeight)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static B2BEdge Deserialize(BlockNode source, BlockNode target, IRelationship relationship)
    {
        return new B2BEdge(source: source, target: target);
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
