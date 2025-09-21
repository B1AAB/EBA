namespace BC2G.Graph.Db.Neo4jDb;

public interface IStrategyFactory : IDisposable
{
    public StrategyBase GetStrategy(GraphComponentType type);
}
