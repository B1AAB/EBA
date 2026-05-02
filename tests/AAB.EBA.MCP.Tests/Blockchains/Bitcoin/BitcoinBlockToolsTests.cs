using AAB.EBA.GraphDb.Tests;
using AAB.EBA.MCP.Blockchains.Bitcoin;
using System.Text.Json.Nodes;

namespace AAB.EBA.MCP.Tests.Blockchains.Bitcoin;

public class BitcoinBlockToolsTests
{
    private readonly BitcoinMcpService _mcpService;
    private readonly BitcoinBlockTools _tools;

    public BitcoinBlockToolsTests()
    {
        var mockDb = GraphMockSeeder.CreateMockDb(BitcoinGraphScenarios.GetCommunity1());
        _mcpService = new BitcoinMcpService(mockDb.Object);
        _tools = new BitcoinBlockTools(_mcpService);
    }

    [Fact]
    public async Task GetBlockInfo_WhenBlockExists_ReturnsCorrectJsonPayload()
    {
        // Act
        var jsonResult = await _tools.GetBlockInfo(height: 10, includeMedianTime: true);

        // Assert
        Assert.NotNull(jsonResult);
        Assert.NotEqual("Block at height 10 not found.", jsonResult);

        // Parse the JSON into a JsonNode
        var node = JsonNode.Parse(jsonResult);
        Assert.NotNull(node); // Prevents possible null reference on node

        // Access the property and ensure it exists before casting
        var medianTimeNode = node["MedianTime"];
        Assert.NotNull(medianTimeNode); // Prevents possible null reference on property

        Assert.Equal(1L, (long)medianTimeNode);
    }
}
