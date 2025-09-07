namespace BC2G.Graph;

public interface IGraphAgent
{
    public Task SampleAsync(CancellationToken ct);
}
