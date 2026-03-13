namespace EBA.Graph.Db.Neo4jDb;

public interface IStrategyFactory : IDisposable
{
    public IReadOnlyDictionary<NodeKind, StrategyBase> NodeStrategies { get; }
    public IReadOnlyDictionary<EdgeKind, StrategyBase> EdgeStrategies { get; }

    public StrategyBase? GetStrategy(NodeKind kind);
    public StrategyBase? GetStrategy(EdgeKind kind);

    public bool IsSerializable(NodeKind kind);
    public bool IsSerializable(EdgeKind kind);

    public Task SerializeConstantsAsync(string outputDirectory, CancellationToken ct);

    /// <summary>
    /// Serialize all the schemas, 
    /// including constraints (e.g., uniqueness of a property) and indexes
    /// that should be run on the database.
    /// </summary>
    public Task SerializeSchemasAsync(string outputDirectory, CancellationToken ct); 
}
