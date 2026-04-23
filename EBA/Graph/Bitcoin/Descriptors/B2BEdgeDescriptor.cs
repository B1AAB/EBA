namespace EBA.Graph.Bitcoin.Descriptors;

public class B2BEdgeDescriptor : IElementDescriptor<B2BEdge>
{
    public static string IdSpace => B2BEdge.Kind.ToString();

    public ElementMapper<B2BEdge> Mapper => StaticMapper;
    public static ElementMapper<B2BEdge> StaticMapper => _mapper;
    private static readonly ElementMapper<B2BEdge> _mapper = new(
        new MappingBuilder<B2BEdge>()
            .MapSourceId(BlockNodeDescriptor.IdSpace, e => e.Height)
            .MapTargetId(BlockNodeDescriptor.IdSpace, e => e.Height)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static B2BEdge Deserialize(BlockNode source, BlockNode target)
    {
        return new B2BEdge(source: source, target: target);
    }

    public string[] Neo4jSeedingOverride
    {
        get
        {
            return
            [
                $"MATCH (target:Block), (source:Block) " +
                $"\r\nWHERE target.{nameof(B2BEdge.Height)} + 1 = source.{nameof(B2BEdge.Height)} " +
                $"\r\nMERGE (target)-[:{RelationType.Follows}]->(source)"
            ];
        }
    }
}
