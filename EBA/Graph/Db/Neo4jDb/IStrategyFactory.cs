namespace EBA.Graph.Db.Neo4jDb;

public interface IStrategyFactory : IDisposable
{
    public StrategyBase GetStrategy(Type type);

    public Task SerializeConstantsAsync(string outputDirectory, CancellationToken ct);

    /// <summary>
    /// Serialize all the schemas, 
    /// including constraints (e.g., uniqueness of a property) and indexes
    /// that should be run on the database.
    /// </summary>
    public Task SerializeSchemasAsync(string outputDirectory, CancellationToken ct); 
}
