namespace BC2G.Graph.Db;

public interface IGraphDb<T> : IDisposable where T : GraphBase
{
    public Task SerializeAsync(T graph, CancellationToken ct);
    public Task ImportAsync(string batchName = "");
}
