using BC2G.Graph.Db.Neo4j.BitcoinMappers;

namespace BC2G.Graph.Db.Neo4j;

public class Neo4jDb : Neo4jDb<BlockGraph> { }

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    private readonly IMapperFactory _mapperFactory;

    public Neo4jDb()
    {
        switch (typeof(T))
        {
            case Type t when t == typeof(BlockGraph):
                _mapperFactory = new MapperFactory();
                break;

            default:
                throw new NotImplementedException(
                    $"A mapper factory of the given type {typeof(T)} is not implemented.");
        }
    }

    public void Import(T g)
    {
        var edgeTypes = g.GetEdges();
        foreach (var type in edgeTypes)
        {
            var first = type.Value.FirstOrDefault();
            if (first == default)
                continue;
            var mapper = _mapperFactory.Get(first);

            foreach (var edge in type.Value)
            {
                mapper.GetCsv(edge);
            }
        }
    }
}
