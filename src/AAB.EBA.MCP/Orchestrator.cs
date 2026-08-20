using AAB.EBA.MCP.CLI;
using AAB.EBA.MCP.Infrastructure;
using AAB.EBA.CLI.Config;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AAB.EBA.MCP;

public class Orchestrator : IDisposable
{
    private readonly Cli _cli;
    private ILogger? _logger;
    private readonly CancellationToken _cT;

    private bool _disposed = false;

    public Orchestrator(CancellationToken cT)
    {
        _cT = cT;

        _cli = new Cli(
            runHandlerAsync: RunAsync,
            exceptionHandler: (e, _) =>
            {
                if (_logger != null)
                    _logger.LogCritical("{error} Inner error: {innerError}", e.Message, e.InnerException?.Message);
                else
                    Console.Error.WriteLine($"Error: {e.Message}");
            });
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        return await _cli.InvokeAsync(args);
    }

    private async Task RunAsync(Options options)
    {
        Directory.CreateDirectory(options.WorkingDir);

        // Clients that launch this process themselves via a "command"/"args" entry
        // (e.g., Claude Desktop, Claude Code, `npx @modelcontextprotocol/inspector <cmd>`)
        // speak MCP over stdin/stdout, so stdio is the default transport.
        // Pass --http to instead run as a standalone Streamable-HTTP server for manual inspector testing
        IHost host = options.Mcp.Http
            ? Startup.GetWebApplication(options)
            : Startup.GetStdioHost(options);

        _logger = host.Services.GetRequiredService<ILogger<Orchestrator>>();
        await host.RunAsync(_cT);
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