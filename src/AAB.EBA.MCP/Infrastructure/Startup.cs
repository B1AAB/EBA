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
    /// over HTTP using the Streamable-HTTP (SSE) transport.
    /// Call <c>app.Run()</c> on the returned instance to start Kestrel.
    /// </summary>
    public static WebApplication GetWebApplication(string[] args, Options options)
    {
        ConfigureSerilog(options);

        var builder = WebApplication.CreateBuilder(args);

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

        return app;
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
                theme: AnsiConsoleTheme.Code)
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