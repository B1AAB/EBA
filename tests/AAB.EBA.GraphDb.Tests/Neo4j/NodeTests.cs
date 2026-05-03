using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver;

namespace AAB.EBA.GraphDb.Tests.Neo4j;

public class NodeTests : IClassFixture<DbFixture>
{
    private readonly Neo4jDb _db;
    private readonly DbFixture _fixture;

    public NodeTests(DbFixture fixture)
    {
        _fixture = fixture;

        var options = new Options { Neo4j = _fixture.Neo4jOptions };

        var logger = NullLogger<Neo4jDb>.Instance;
        _db = new Neo4jDb(options, logger);
    }

    [Fact]
    public async Task GetNodeAsync_WhenExactlyOneNodeExists_ReturnsNode()
    {
        // Act
        var node = await _db.GetNodeAsync(
            label: ScriptNode.Kind,
            propertyKey: "SHA256Hash",
            propertyValue: "hash1",
            ct: CancellationToken.None);

        // Assert
        Assert.NotNull(node);
        Assert.Equal("hash1", node.Properties["SHA256Hash"].As<string>());
        Assert.Equal("PubKey", node.Properties["ScriptType"].As<string>());
    }

    [Fact]
    public async Task GetNodeAsync_WhenNodeDoesNotExist_ReturnsNull()
    {
        // Act
        var node = await _db.GetNodeAsync(
            label: ScriptNode.Kind,
            propertyKey: "SHA256Hash",
            propertyValue: "missing_hash",
            ct: CancellationToken.None);

        // Assert
        Assert.Null(node);
    }

    [Fact]
    public async Task FindNodesAsync_FindLatestNode_ReturnHeight()
    {
        // Act
        var nodes = await _db.FindNodesAsync(
            nodeKind: BlockNode.Kind,
            ct: CancellationToken.None,
            orderByProperty: "Height",
            descending: true,
            limit: 1);

        // Assert
        Assert.NotNull(nodes);
        Assert.Single(nodes);

        var height = nodes[0].Properties["Height"].As<long>();

        Assert.Equal(50, height);
    }
}
