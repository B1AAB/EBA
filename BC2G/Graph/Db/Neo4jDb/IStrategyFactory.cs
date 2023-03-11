namespace BC2G.Graph.Db.Neo4jDb;

public interface IStrategyFactory
{
    public IStrategyBase GetStrategyBase(string type);
    public INodeStrategy GetNodeStrategy(string type);
    public IEdgeStrategy GetEdgeStrategy(string type);
    public IGraphStrategy GetGraphStrategy(string type);
}
