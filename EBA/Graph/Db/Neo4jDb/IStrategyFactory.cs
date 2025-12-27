namespace EBA.Graph.Db.Neo4jDb;

public interface IStrategyFactory : IDisposable
{
    public StrategyBase GetStrategy(GraphComponentType type);

    public Task SerializeConstantsAsync(string outputDirectory, CancellationToken ct);
}
