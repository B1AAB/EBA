namespace BC2G.Graph.Db.Neo4j;

public interface IMapperFactory
{
    public IMapper Get(string type);
}
