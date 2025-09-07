namespace BC2G.Graph.Db;

public interface IGraphDb<T> : IDisposable where T : GraphBase
{
    public Task SerializeAsync(T graph, CancellationToken ct);
    public Task ImportAsync(CancellationToken ct, string batchName = "", List<GraphComponentType>? importOrder = null);
    public Task SampleAsync(CancellationToken ct);
    public void ReportQueries();

    // TODO: instead of a string, nodeType should be an enum.
    public Task<List<Model.INode>> GetRandomNodes(string nodeType, int count, double nodeSelectProbability = 0.1);
}
