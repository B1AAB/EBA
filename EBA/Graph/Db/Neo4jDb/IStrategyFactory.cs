namespace EBA.Graph.Db.Neo4jDb;

public interface IStrategyFactory : IDisposable
{
    public IReadOnlyDictionary<NodeKind, IElementStrategy> NodeStrategies { get; }
    public IReadOnlyDictionary<EdgeKind, IElementStrategy> EdgeStrategies { get; }

    public IElementStrategy? GetStrategy(NodeKind kind);
    public IElementStrategy? GetStrategy(EdgeKind kind);

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
