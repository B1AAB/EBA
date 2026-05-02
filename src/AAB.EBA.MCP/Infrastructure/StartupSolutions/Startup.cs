using AAB.EBA.GraphDb;
using AAB.EBA.MCP.Blockchains.Bitcoin;
using EBA.CLI.Config;
using EBA.Graph.Bitcoin.Descriptors;
using EBA.Graph.Db.Neo4jDb;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AAB.EBA.MCP.Infrastructure.StartupSolutions;

public class Startup
{
    public static HostBuilder GetHostBuilder(Options options)
    {
        var hostBuilder = new HostBuilder();

        var logFilename = options.Logger.LogFilename;

        Log.Logger =
            new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override(
                "System.Net.Http.HttpClient",
                Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFilename,
                rollingInterval: RollingInterval.Hour,
                outputTemplate: options.Logger.MessageTemplate,
                shared: true,
                retainedFileCountLimit: null)
            /*.WriteTo.Console(
                theme: AnsiConsoleTheme.Code)*/
            .CreateLogger();
        hostBuilder.UseSerilog();

        hostBuilder.ConfigureAppConfiguration(
            (hostingContext, configuration) =>
            {
                ConfigureApp(hostingContext, configuration, options);
            });

        hostBuilder.ConfigureServices(
            services =>
            {
                ConfigureServices(services, options);
            });

        return hostBuilder;
    }

    private static void ConfigureApp(
        HostBuilderContext context,
        IConfigurationBuilder config,
        Options options)
    {
        config.Sources.Clear();
        var env = context.HostingEnvironment;

        config
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile(
                $"appsettings.json",
                optional: true,
                reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{env.EnvironmentName}.json",
                optional: true,
                reloadOnChange: true);

        var configRoot = config.Build();
        configRoot.GetSection(nameof(Options)).Bind(options);
    }

    private static void ConfigureServices(IServiceCollection services, Options options)
    {
        services.AddSingleton(options);
        services.AddSingleton<BitcoinMcpService>();
        services.AddSingleton<IStrategyFactory, BitcoinStrategyFactory>();
        services.AddSingleton<IGraphDb, Neo4jDb>();

        services.AddHttpClient();

        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
    }
}