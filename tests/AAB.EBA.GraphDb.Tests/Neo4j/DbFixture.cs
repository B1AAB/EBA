using EBA.CLI.Config;
using Neo4j.Driver;
using Testcontainers.Neo4j;

namespace AAB.EBA.GraphDb.Tests.Neo4j;

public class DbFixture : IAsyncLifetime
{
    public Neo4jContainer Neo4jContainer { get; private set; } = null!;
    public Neo4jOptions Neo4jOptions {  get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var username = "neo4j";
        var password = "testpassword";
        Neo4jContainer = new Neo4jBuilder("neo4j:5")
            .WithEnvironment("NEO4J_AUTH", $"{username}/{password}")
            .Build();

        await Neo4jContainer.StartAsync();

        Neo4jOptions = new Neo4jOptions()
        {
            User = username,
            Password = password,
            Uri = Neo4jContainer.GetConnectionString()
        };

        // connect to docker
        var driver = GraphDatabase.Driver(
            Neo4jContainer.GetConnectionString(), 
            AuthTokens.Basic(Neo4jOptions.User, Neo4jOptions.Password));

        var testGraph = BitcoinGraphScenarios.GetCommunity1();
        await GraphAdapter.ToNeo4jAsync(driver, testGraph);
    }

    public async ValueTask DisposeAsync()
    {
        if (Neo4jContainer != null)
            await Neo4jContainer.DisposeAsync();
        
        GC.SuppressFinalize(this);
    }
}
