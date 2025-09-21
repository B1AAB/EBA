namespace BC2G.Graph.Db;

public interface IGraphDb<T> : IDisposable where T : GraphBase
{
    public Task SerializeAsync(T graph, CancellationToken ct);
    public Task ImportAsync(CancellationToken ct, string batchName = "", List<GraphComponentType>? importOrder = null);
    public Task<bool> TrySampleAsync();
    public void ReportQueries();
}
