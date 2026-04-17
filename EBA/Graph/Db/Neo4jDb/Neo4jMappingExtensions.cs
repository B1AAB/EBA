namespace EBA.Graph.Db.Neo4jDb;

public static class Neo4jMappingExtensions
{
    public static MappingBuilder<T> MapNeo4jTargetId<T, TProperty>(
        this MappingBuilder<T> builder,
        string idSpace,
        Func<T, TProperty> selector)
    {
        return builder.Map(
            new PropertyMapping<T>(
                ":END_ID",
                MappingBuilder.ToFieldType(typeof(TProperty)),
                x => selector(x),
                _ => $":END_ID({idSpace})"));
    }
}
