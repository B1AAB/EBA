namespace BC2G.Graph.Db.Neo4jDb;

public interface IMapperFactory
{
    public IMapperBase GetMapperBase(string type);
    public IEdgeMapper GetEdgeMapper(string type);
    public IGraphMapper GetGraphMapper(string type);
}
