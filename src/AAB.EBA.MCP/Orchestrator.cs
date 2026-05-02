using AAB.EBA.MCP.Infrastructure.StartupSolutions;
using EBA.CLI.Config;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AAB.EBA.MCP;

public class Orchestrator : IDisposable
{
    private ILogger? _logger;
    private readonly CancellationToken _cT;

    private bool _disposed = false;

    public Orchestrator(CancellationToken cT)
    {
        _cT = cT;
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        var options = new Options();
        var host = await SetupAndGetHostAsync(options);

        await host.RunAsync(_cT);

        return 0;
    }

    private async Task<IHost> SetupAndGetHostAsync(Options options)
    {
        Directory.CreateDirectory(options.WorkingDir);
        var hostBuilder = Startup.GetHostBuilder(options);
        var host = hostBuilder.Build();
        _logger = host.Services.GetRequiredService<ILogger<Orchestrator>>();

        return host;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            { }

            _disposed = true;
        }
    }
}