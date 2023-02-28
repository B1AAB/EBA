namespace BC2G.Graph.Db.Neo4jDb;

public interface IStrategyFactory
{
    public IMapperBase GetStrategyBase(string type);
    public IEdgeMapper GetEdgeStrategy(string type);
    public IGraphMapper GetGraphStrategy(string type);
}
