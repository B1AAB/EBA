using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin;
using AAB.EBA.Graph.Db;
using AAB.EBA.Graph.Db.Neo4jDb;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Graph;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// Runs <see cref="BitcoinOrchestrator.TraverseAsync"/> the way the CLI does,
/// with the production orchestrators and graph database, except that the
/// Bitcoin node is replaced by the given HTTP handler (e.g., a
/// <see cref="SyntheticBitcoinNode"/>), and the graph database can be
/// decorated (e.g., by an <see cref="InterceptingGraphDb{T}"/>).
///
/// Every call to <see cref="RunAsync"/> builds a new host and disposes it
/// when the traverse ends, as every run of the CLI is a new process. This is
/// required because a traverse disposes the (singleton) graph database at
/// its end, hence a host cannot be used for more than one traverse.
/// </summary>
public sealed class TraverseRunner(
    Options options,
    HttpMessageHandler bitcoinNode,
    Func<IGraphDb<BitcoinGraph>, IGraphDb<BitcoinGraph>>? decorateGraphDb = null)
{
    public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMinutes(1);

    private readonly Options _options = options;
    private readonly HttpMessageHandler _bitcoinNode = bitcoinNode;
    private readonly Func<IGraphDb<BitcoinGraph>, IGraphDb<BitcoinGraph>> _decorateGraphDb =
        decorateGraphDb ?? (x => x);

    /// <summary>
    /// Invoked when the graph database is resolved from the host, which first
    /// happens when the traverse builds its graph buffer: after the queues of
    /// blocks are persisted, and before any block is requested.
    /// </summary>
    public Action? OnGraphDbResolved { get; init; }

    /// <summary>
    /// If set, the logs of the traverse are routed to this provider.
    /// </summary>
    public ILoggerProvider? LoggerProvider { get; init; }

    /// <summary>
    /// Runs the traverse and returns when it ends; it throws whatever the
    /// traverse throws (e.g., <see cref="OperationCanceledException"/> when
    /// canceled), or a <see cref="TimeoutException"/> if the traverse does not
    /// end within <paramref name="timeout"/> (default: <see cref="DefaultTimeout"/>),
    /// so that a traverse that never ends fails the test instead of hanging it.
    /// </summary>
    public async Task RunAsync(CancellationToken ct, TimeSpan? timeout = null)
    {
        Directory.CreateDirectory(_options.WorkingDir);

        using var host = BuildHost();
        var orchestrator = host.Services.GetRequiredService<BitcoinOrchestrator>();

        await orchestrator
            .TraverseAsync(_options, ct)
            .WaitAsync(timeout ?? DefaultTimeout, CancellationToken.None);
    }

    /// <summary>
    /// Runs the traverse expecting it to be canceled through <paramref name="ct"/>,
    /// and returns when it ends. It throws if the traverse ends for any other
    /// reason (e.g., it completes without being canceled, fails, or times out),
    /// so tests asserting on the output of a canceled traverse cannot pass
    /// on the output of a traverse that was not canceled.
    /// </summary>
    public async Task RunUntilCanceledAsync(CancellationToken ct, TimeSpan? timeout = null)
    {
        try
        {
            await RunAsync(ct, timeout);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }

        throw new InvalidOperationException("The traverse completed without being canceled.");
    }

    private IHost BuildHost()
    {
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(builder =>
                {
                    if (LoggerProvider is not null)
                        builder.AddProvider(LoggerProvider);
                });
                services.AddSingleton(_options);
                services.AddSingleton<BitcoinOrchestrator>();
                services.AddSingleton<BitcoinGraphOrchestrator>();

                services.AddSingleton(sp =>
                {
                    OnGraphDbResolved?.Invoke();
                    return _decorateGraphDb(
                        new BitcoinNeo4jDb(
                            _options,
                            sp.GetRequiredService<ILogger<Neo4jDb<BitcoinGraph>>>()));
                });

                services.AddTransient(sp => new BitcoinChainAgent(
                    new HttpClient(_bitcoinNode, disposeHandler: false)
                    {
                        BaseAddress = _options.Bitcoin.Traverse.ClientUri
                    },
                    sp.GetRequiredService<ILogger<BitcoinChainAgent>>()));
            })
            .Build();
    }
}
