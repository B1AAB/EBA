namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

public class MapperFactory : IMapperFactory
{
    public IMapper Get(string type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return type switch
        {
            string x when x == Utilities.TypeToString<S2SEdge>() => new S2SEdgeMapper(),
            string x when x == Utilities.TypeToString<T2TEdge>() => new T2TEdgeMapper(),
            _ => throw new NotImplementedException(
                $"A mapper for type {type} is not implemented."),
        };
    }
}
