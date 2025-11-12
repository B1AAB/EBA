using EBA.CLI;
using EBA.Graph.Bitcoin;
using EBA.Utilities;

using EBA.CLI;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace EBA;

public class Orchestrator : IDisposable
{
    private readonly Cli _cli;
    private ILogger? _logger;
    private readonly CancellationToken _cT;

    private bool _disposed = false;

    public Orchestrator(CancellationToken cancelationToken)
    {
        _cT = cancelationToken;

        _cli = new Cli(
            TraverseBitcoinAsync,
            SampleGraphAsync,
            ImportGraphAsync,
            AddressStatsAsync,
            ImportCypherQueriesAsync,
            (e, c) =>
            {
                if (_logger != null)
                    _logger.LogCritical("{error}", e.Message);
                else
                    Console.Error.WriteLine($"Error: {e.Message}");
            });
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        return await _cli.InvokeAsync(args);
    }

    private async Task<IHost> SetupAndGetHostAsync(Options options)
    {
        Directory.CreateDirectory(options.WorkingDir);
        var hostBuilder = Startup.GetHostBuilder(options);
        var host = hostBuilder.Build();
        await host.StartAsync();
        _logger = host.Services.GetRequiredService<ILogger<Orchestrator>>();
        return host;
    }

    private async Task TraverseBitcoinAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        var bitcoinOrchestrator = host.Services.GetRequiredService<BitcoinOrchestrator>();
        await bitcoinOrchestrator.TraverseAsync(options, _cT);
    }

    private async Task ImportGraphAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);

        var graphDb = host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>();
        await graphDb.ImportAsync(_cT);
    }

    private async Task ImportCypherQueriesAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);

        var graphDb = host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>();
        graphDb.ReportQueries();
    }

    private async Task SampleGraphAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);

        if (options.Bitcoin.UseLegacyGraphDbMethod)
        {
            var graphDb = host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>();
            await graphDb.SampleAsync(_cT);
        }
        else
        {
            var bitcoinGraphAgent = host.Services.GetRequiredService<GraphAgent>();
            await bitcoinGraphAgent.SampleAsync(_cT);
        }
    }

    private async Task AddressStatsAsync(Options options)
    {
        _ = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);

        _logger?.LogWarning("This command runs an in-memory process that may need significant memory.");

        //var newAddressCounter = new NewAddressCounter(_logger);
        //newAddressCounter.Analyze(options.Bitcoin.PerBlockAddressesFilename, options.Bitcoin.StatsFilename, options.WorkingDir, _cT);
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