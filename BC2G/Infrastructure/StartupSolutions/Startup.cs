using BC2G.Graph.Db.Neo4j;

namespace BC2G.Infrastructure.StartupSolutions;

public class Startup
{
    public static HostBuilder GetHostBuilder(Options options)
    {
        var hostBuilder = new HostBuilder();

        // Setup logging using Serilog.
        var logFilename = Path.Join(
            options.WorkingDir,
            options.Logger.RepoName +
            DateTimeOffset.Now.ToUnixTimeSeconds().ToString() +
            ".log");

        Log.Logger =
            new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override(
                "System.Net.Http.HttpClient",
                Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(
                "Microsoft.EntityFrameworkCore.Database.Command",
                Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(
                "Microsoft.EntityFrameworkCore.Database.Connection",
                Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(
                "Microsoft.EntityFrameworkCore.Infrastructure",
                Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFilename,
                rollingInterval: RollingInterval.Hour,
                outputTemplate: options.Logger.MessageTemplate)
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
        services.AddSingleton<GraphDb>();
        services.AddSingleton<IGraphDb<BitcoinBlockGraph>, BitcoinNeo4jDb>();
        services.AddSingleton<BitcoinOrchestrator>();

        // Passing BitcoinAgent type as the generic argument
        // to AddHttpClient will cause registering it 
        // with a transient scope in DI. Additionally, since
        // BitcoinAgent requires an HttpClient in the constructor,
        // it will be wired up within the factory such that
        // every new instance of BitcoinAgent is created
        // with the appropriately configured HttpClient injected in.
        services.AddHttpClient<BitcoinAgent>(client =>
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

        // I am using DbContextFactory instead of DbContext because
        // an scoped instance of database (mainly in BitcoinAgent)
        // is used for multiple unit-of-work, and it is recommended
        // to use an scoped instace of DbContext for one unit-of-work
        // only. See the following link on details.
        // https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
        services.AddDbContextFactory<DatabaseContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(
                $"Host={options.Psql.Host};" +
                $"Database={options.Psql.Database};" +
                $"Username={options.Psql.Username};" +
                $"Password={options.Psql.Password}");

            // Read these docs on the effect of the following settings: 
            // https://www.npgsql.org/doc/connection-string-parameters.html
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        });
    }
}
