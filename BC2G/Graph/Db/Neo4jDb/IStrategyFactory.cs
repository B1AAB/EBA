namespace BC2G.Graph.Db.Neo4jDb;

public interface IStrategyFactory
{
    public IStrategyBase GetStrategyBase(GraphComponentType type);
    public INodeStrategy GetNodeStrategy(GraphComponentType type);
    public IEdgeStrategy GetEdgeStrategy(GraphComponentType type);
    public IGraphStrategy GetGraphStrategy(GraphComponentType type);
}
