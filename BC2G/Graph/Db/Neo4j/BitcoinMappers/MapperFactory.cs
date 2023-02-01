namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

public class MapperFactory : IMapperFactory
{
    public IMapper<T> Get<T>(T entity)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(S2SEdge) => (IMapper<T>)new S2SEdgeMapper(),
            _ => throw new NotImplementedException(),
        };
    }
}
