using BC2G.Blockchains.Bitcoin.Graph;
using BC2G.Graph.Db.Neo4j;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BC2G;

public class Orchestrator : IDisposable
{
    private readonly CLI _cli;
    private readonly IHost _host;
    private readonly Options _options;
    private readonly CancellationToken _cT;

    private bool disposed = false;

    public ILogger Logger { get; }

    public Orchestrator(IHost host, Options options, CancellationToken cancelationToken)
    {
        _host = host;
        _cT = cancelationToken;
        _options = options;
        Logger = _host.Services.GetRequiredService<ILogger<Orchestrator>>();
        _cli = new CLI(
            _options,
            TraverseBitcoinAsync,
            SampleGraphAsync,
            LoadGraphAsync,
            (e, c) =>
            {
                Logger?.LogCritical("{error}", e.Message);
            });
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        return await _cli.InvokeAsync(args);
    }

    private async Task SampleGraphAsync()
    {
        await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile, _cT);
        var graphDb = _host.Services.GetRequiredService<GraphDb>();
        var successfull = await graphDb.TrySampleAsync();
        if (successfull)
            Logger.LogInformation("Successfully completed sampling graphs.");
    }

    private async Task LoadGraphAsync()
    {
        await JsonSerializer<Options>.SerializeAsync(_options, _options.StatusFile, _cT);

        var graphDb = _host.Services.GetRequiredService<IGraphDb<BitcoinBlockGraph>>();
        await graphDb.ImportAsync();

        var graphDbOld = _host.Services.GetRequiredService<GraphDb>();
        graphDbOld.BulkImport(_options.WorkingDir);
    }

    private async Task TraverseBitcoinAsync()
    {
        using (var dbContext = _host.Services.GetRequiredService<DatabaseContext>())
            await dbContext.Database.EnsureCreatedAsync(_cT);

        var bitcoinOrchestrator = _host.Services.GetRequiredService<BitcoinOrchestrator>();
        await bitcoinOrchestrator.TraverseAsync(_options, _cT);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            { }

            disposed = true;
        }
    }
}
