namespace AAB.EBA.Graph.Db.Neo4jDb;

public interface IStrategyFactory : IDisposable
{
    public IReadOnlyDictionary<NodeKind, IElementCodec> NodeStrategies { get; }
    public IReadOnlyDictionary<EdgeKind, IElementCodec> EdgeStrategies { get; }

    public IElementCodec? GetStrategy(NodeKind kind);
    public IElementCodec? GetStrategy(EdgeKind kind);

    public bool IsSerializable(NodeKind kind);
    public bool IsSerializable(EdgeKind kind);

    public Task SerializeConstantsAsync(string outputDirectory, CancellationToken ct);

    /// <summary>
    /// Serialize all the schemas, 
    /// including constraints (e.g., uniqueness of a property) and indexes
    /// that should be run on the database.
    /// </summary>
    public Task SerializeSchemasAsync(string outputDirectory, CancellationToken ct);

    bool TryCreateNode<T>(
            Neo4j.Driver.INode node,
            out T createdNode,
            double? originalIndegree = null,
            double? originalOutdegree = null,
            double? outHopsFromRoot = null) where T : Model.INode;

    bool TryCreateNode(
        Neo4j.Driver.INode node,
        out Model.INode createdNode,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? outHopsFromRoot = null);

    bool TryCreateNodes<T>(
        List<IRecord> records,
        out List<T> nodes,
        string nodeVar = "n") where T : Model.INode;

    IEdge<Model.INode, Model.INode> CreateEdge(
        Model.INode source,
        Model.INode target,
        IRelationship neo4jRelationship);
}
