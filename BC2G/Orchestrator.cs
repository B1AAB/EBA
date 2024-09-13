using BC2G.CLI;
using BC2G.Utilities.Utilities;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BC2G;

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

        if (options.Bitcoin.UseTxDatabase)
        {
            using var dbContext = host.Services.GetRequiredService<DatabaseContext>();
            try
            {
                await dbContext.Database.EnsureCreatedAsync(_cT);
            }
            catch (NpgsqlException e)
            {
                _logger?.LogError(
                    "Cannot connect to database; make sure PostgreSQL 16 is installed and listening on port {p}. {e}",
                    5432, e.Message);
                throw;
            }
        }

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

    private async Task SampleGraphAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);
        var graphDb = host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>();
        var successfull = await graphDb.TrySampleAsync();
        if (successfull)
            _logger?.LogInformation("Successfully completed sampling graphs.");
        else
            _logger?.LogError("Faild sampling graphs with the given parameters.");
    }

    private async Task AddressStatsAsync(Options options)
    {
        _ = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);

        _logger?.LogWarning("This command runs an in-memory process that may need significant memory.");

        var newAddressCounter = new NewAddressCounter(_logger);
        newAddressCounter.Analyze(options.Bitcoin.PerBlockAddressesFilename, options.Bitcoin.StatsFilename, options.WorkingDir, _cT);
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
