using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db;

public interface IGraphDb<T> : IDisposable where T : GraphBase
{
    public Task VerifyConnectivityAsync(CancellationToken ct);
    public Task SerializeAsync(T graph, CancellationToken ct);
    public Task ImportAsync(CancellationToken ct, string batchName = "", List<GraphComponentType>? importOrder = null);
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
        string propKey,
        string propValue,
        int queryLimit,
        string labelFilters,
        int maxLevel,
        GraphTraversal traversalAlgorithm,
        string relationshipFilter = "");
}