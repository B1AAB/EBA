namespace EBA.Graph;

public interface IGraphOrchestrator<T> where T : GraphBase
{
    public Task SampleAsync(CancellationToken ct);
    public Task SerializeAsync(T g, CancellationToken ct);
}