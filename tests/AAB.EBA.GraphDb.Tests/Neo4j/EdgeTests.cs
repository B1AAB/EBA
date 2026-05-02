using EBA.Blockchains.Bitcoin.GraphModel;
using EBA.CLI.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver;

namespace AAB.EBA.GraphDb.Tests.Neo4j;

public class EdgeTests : IClassFixture<DbFixture>
{
    private readonly Neo4jDb _db;
    private readonly DbFixture _fixture;

    public EdgeTests(DbFixture fixture)
    {
        _fixture = fixture;

        var options = new Options { Neo4j = _fixture.Neo4jOptions };

        var logger = NullLogger<Neo4jDb>.Instance;
        _db = new Neo4jDb(options, logger);
    }

    [Fact]
    public async Task GetEdgesAsyn_ReturnsAllIncomingAndOutgoingEdges_ForTargetNode()
    {
        // Act
        var edges = await _db.GetEdgesAsync(
            nodeKind: ScriptNode.Kind,
            nodePropertyKey: "SHA256Hash",
            nodePropertyValue: "hash1",
            ct: CancellationToken.None);

        // Assert
        Assert.NotNull(edges);
        Assert.Equal(3, edges.Count);

        var relationshipTypes = edges.Select(e => e.Type).ToList();
        Assert.Contains(T2SEdge.Kind.ToString(), relationshipTypes);

        var edgeValues = edges.Select(e => e.Properties["Value"].As<long>()).ToList();
        Assert.Contains(50, edgeValues);
    }
}
