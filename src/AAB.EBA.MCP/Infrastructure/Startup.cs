using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.GraphDb;
using AAB.EBA.MCP.Blockchains.Bitcoin;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace AAB.EBA.MCP.Infrastructure;

public class Startup
{
    /// <summary>
    /// Builds and configures a <see cref="WebApplication"/> that exposes the MCP server
    /// over HTTP using the Streamable-HTTP (SSE) transport. Intended for manual testing
    /// via the MCP inspector, pointed at a standalone, already-running instance.
    /// Call <c>app.Run()</c> on the returned instance to start Kestrel.
    /// </summary>
    public static WebApplication GetWebApplication(Options options)
    {
        ConfigureSerilog(options);

        var builder = WebApplication.CreateBuilder();

        builder.Host.UseSerilog();

        builder.Configuration.Sources.Clear();
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        builder.Configuration.GetSection(nameof(Options)).Bind(options);

        ConfigureCommonServices(builder.Services, options);

        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => { options.Stateless = true; })
            .WithToolsFromAssembly();

        var app = builder.Build();

        app.MapMcp();

        app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

        return app;
    }

    /// <summary>
    /// Builds and configures an <see cref="IHost"/> that exposes the MCP server over
    /// stdin/stdout. This is the transport expected by clients (Claude Desktop, Claude
    /// Code, etc.) that launch the server themselves via a "command"/"args" entry, so it
    /// is the default when running <c>AAB.EBA.MCP.dll</c> directly.
    /// Call <c>host.RunAsync()</c> on the returned instance to start it.
    /// </summary>
    public static IHost GetStdioHost(Options options)
    {
        ConfigureSerilog(options);

        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSerilog();

        builder.Configuration.Sources.Clear();
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        builder.Configuration.GetSection(nameof(Options)).Bind(options);

        ConfigureCommonServices(builder.Services, options);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        return builder.Build();
    }

    private static void ConfigureSerilog(Options options)
    {
        Log.Logger =
            new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override(
                "System.Net.Http.HttpClient",
                Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: options.Logger.LogFilename,
                rollingInterval: RollingInterval.Hour,
                outputTemplate: options.Logger.MessageTemplate,
                shared: true,
                retainedFileCountLimit: null)
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                // The stdio MCP transport uses stdout exclusively for JSON-RPC framing;
                // any other text written there (e.g. these log lines) corrupts the
                // protocol stream. Routing everything to stderr instead is a no-op for
                // the HTTP transport and required for the stdio one.
                standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();
    }

    private static void ConfigureCommonServices(IServiceCollection services, Options options)
    {
        services.AddSingleton(options);
        services.AddSingleton<BitcoinMcpService>();
        services.AddSingleton<IStrategyFactory, BitcoinStrategyFactory>();
        services.AddSingleton<IGraphDb, Neo4jDb>();

        // TODO: this is a hack. Need it to access strategy factory from the service
        services.AddSingleton<IGraphDb<BitcoinGraph>, BitcoinNeo4jDb>();

        services.AddHttpClient();
    }
}