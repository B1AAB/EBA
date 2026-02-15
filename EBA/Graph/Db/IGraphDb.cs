using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db;

public interface IGraphDb<T> : IDisposable where T : GraphBase
{
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

    public Task<List<IRecord>> GetRandomNodesAsync(
        NodeLabels label,
        int count,
        CancellationToken ct,
        double rootNodeSelectProbability = 0.1,
        string nodeVariable = "randomNode");

    public Task<List<IRecord>> GetNeighborsAsync(
        NodeLabels rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int queryLimit,
        int maxLevel,
        bool useBFS,
        CancellationToken ct,
        string relationshipFilter = "");
}