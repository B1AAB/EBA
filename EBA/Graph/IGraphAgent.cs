namespace EBA.Graph;

public interface IGraphAgent
{
    public Task SampleAsync(CancellationToken ct);
}