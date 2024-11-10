using BC2G.Graph.Db.Neo4jDb;

namespace BC2G.Infrastructure.StartupSolutions;

public class Startup
{
    public static HostBuilder GetHostBuilder(Options options)
    {
        var hostBuilder = new HostBuilder();

        // Setup logging using Serilog.
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
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code)
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
        services.AddSingleton<IGraphDb<BitcoinGraph>, BitcoinNeo4jDb>();
        services.AddSingleton<BitcoinOrchestrator>();

        // Passing BitcoinAgent type as the generic argument
        // to AddHttpClient will cause registering it 
        // with a transient scope in DI. Additionally, since
        // BitcoinAgent requires an HttpClient in the constructor,
        // it will be wired up within the factory such that
        // every new instance of BitcoinAgent is created
        // with the appropriately configured HttpClient injected in.
        services
            .AddHttpClient<BitcoinAgent>(client =>
            {
                client.BaseAddress = options.Bitcoin.ClientUri;
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "BC2G");
                client.Timeout = options.Bitcoin.HttpClientTimeout;
            })
            .AddPolicyHandler((provider, _) =>
            {
                return ResilienceStrategyFactory.Bitcoin.GetClientStrategy(
                    provider,
                    options.Bitcoin.HttpClientResilienceStrategy);
            });

        // This sets the limit for all the endpoints globally. 
        ServicePointManager.DefaultConnectionLimit = options.DefaultConnectionLimit;
    }
}
