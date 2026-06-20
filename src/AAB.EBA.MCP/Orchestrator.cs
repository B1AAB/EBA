using AAB.EBA.MCP.Infrastructure;
using AAB.EBA.CLI.Config;
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

        Directory.CreateDirectory(options.WorkingDir);
        var app = Startup.GetWebApplication(args, options);
        _logger = app.Services.GetRequiredService<ILogger<Orchestrator>>();
        await app.RunAsync(_cT);

        return 0;
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