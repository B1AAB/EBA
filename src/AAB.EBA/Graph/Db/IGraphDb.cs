using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Utilities;

namespace AAB.EBA.Graph.Db;

public interface IGraphDb<T> : IDisposable, IAsyncDisposable where T : GraphBase
{
    public IStrategyFactory StrategyFactory { get; }

    public Task VerifyConnectivityAsync(CancellationToken ct);
    public Task SerializeAsync(T graph, CancellationToken ct);

    /// <summary>
    /// Serializes constant graph components like
    /// constraints and indexes, or unique nodes and relationships 
    /// that need to be present in a graph belonging to a given 
    /// blockchain (e.g., a single `coinbase` node).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns></returns>
    public Task SerializeConstantsAndConstraintsAsync(CancellationToken ct);
    public Task ImportAsync(CancellationToken ct, string batchName = "");
    public Task SampleAsync(CancellationToken ct);
    public void ReportQueries();


    public Task BulkUpdateNodePropertiesAsync(
        NodeKind label,
        string idProperty,
        IReadOnlyList<Dictionary<string, object?>> updates,
        CancellationToken ct);

    public Task<List<IRecord>> GetRandomNodesAsync(
        NodeKind label,
        int count,
        CancellationToken ct,
        double rootNodeSelectProbability = 0.1,
        string nodeVariable = "randomNode");

    public Task<List<IRecord>> GetNeighborhoodAsync(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int queryLimit,
        int maxLevel,
        bool useBFS,
        CancellationToken ct,
        string relationshipFilter = "");

    /// <summary>
    /// Returns the exact in/out degree of one node and, for each given inclusive height
    /// range, how many of its relationships have their activity height inside it. The
    /// range counts are computed in one pass over at most <paramref name="scanLimit"/> of
    /// the node's relationships, so the cost is bounded for high-degree nodes; the result
    /// says how many relationships were scanned (see NodeActivityCounts). Returns zero
    /// counts when the node has no relationships. Throws if the node does not exist.
    /// </summary>
    public Task<NodeActivityCounts> GetNodeActivityCountsAsync(
        NodeKind label,
        string idProperty,
        string id,
        IReadOnlyList<BlockHeightRange> ranges,
        int scanLimit,
        CancellationToken ct);

    public Task<List<IRecord>> GetNodesAsync(
        NodeKind label,
        CancellationToken ct,
        string nodeVariable = "n",
        int? count = null);

    public Task<List<IRecord>> GetEdgesAsync(
        EdgeKind edgeKind,
        CancellationToken ct,
        string sourceNodeVariable = "source",
        string targetNodeVariable = "target",
        string relationshipVariable = "relationship",
        string? sourceNodeIdProperty = null,
        object? sourceNodeId = null,
        string? targetNodeIdProperty = null,
        object? targetNodeId = null);

    public Task ExecuteWriteQueryAsync(
        List<string> schemas, 
        CancellationToken ct);
}