using AAB.EBA.GraphDb.Tests;
using AAB.EBA.MCP.Blockchains.Bitcoin;

namespace AAB.EBA.MCP.Tests.Blockchains.Bitcoin;

public class BitcoinMcpServiceTests
{
    [Fact]
    public async Task GetScriptBalance_WhenNodeExists_ReturnsMappedScriptNode()
    {
        // Arrange
        var graph = BitcoinGraphScenarios.GetCommunity1();
        var mockDb = GraphMockSeeder.CreateMockDb(graph);
        //var ops = new Options { Neo4j = new Neo4jOptions { CompressOutput = false } };
        //var realFactory = new BitcoinStrategyFactory(ops);
        var service = new BitcoinMcpService(mockDb.Object);

        // Act
        var balance = await service.GetScriptBalanceAsync("hash1", ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(balance);
        Assert.Equal(50, balance);
    }

    [Fact]
    public async Task GetScriptByAddress_WhenValidAddress_ReturnScriptNode()
    {
        // Arrange
        var graph = BitcoinGraphScenarios.GetCommunity1();
        var mockDb = GraphMockSeeder.CreateMockDb(graph);
        //var ops = new Options { Neo4j = new Neo4jOptions { CompressOutput = false } };
        //var realFactory = new BitcoinStrategyFactory(ops);
        var service = new BitcoinMcpService(mockDb.Object);

        // Act
        var scriptNode = await service.GetScriptByAddressAsync("add1", ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(scriptNode);
        Assert.Equal("add1", scriptNode.Address);
        Assert.Equal("hash1", scriptNode.SHA256Hash);
    }

    [Fact]
    public async Task GetScriptBySHA256_WhenValidSHA256_ReturnScriptNode()
    {
        // Arrange
        var graph = BitcoinGraphScenarios.GetCommunity1();
        var mockDb = GraphMockSeeder.CreateMockDb(graph);
        //var ops = new Options { Neo4j = new Neo4jOptions { CompressOutput = false } };
        //var realFactory = new BitcoinStrategyFactory(ops);
        var service = new BitcoinMcpService(mockDb.Object);

        // Act
        var scriptNode = await service.GetScriptBySHA256Async("hash1", ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(scriptNode);
        Assert.Equal("add1", scriptNode.Address);
        Assert.Equal("hash1", scriptNode.SHA256Hash);
    }

    [Fact]
    public async Task GetScriptTxSummaryStats_WhenValidSHA_ReturnStats()
    {
        // Arrange
        var graph = BitcoinGraphScenarios.GetCommunity1();
        var mockDb = GraphMockSeeder.CreateMockDb(graph);
        var service = new BitcoinMcpService(mockDb.Object);

        // Act
        var stats = await service.GetScriptTxSummaryStatsAsync("hash1", ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TxCount);

        Assert.Equal(75, stats.TotalReceived);
        Assert.Equal(25, stats.TotalSent);

        Assert.Equal(20, stats.FirstReceivedHeight);
        Assert.Equal(25, stats.FirstReceivedValue);

        Assert.Equal(20, stats.FirstSentHeight);
        Assert.Equal(25, stats.FirstSentValue);

        Assert.Equal(30, stats.LastReceivedHeight);
        Assert.Equal(50, stats.LastReceivedValue);

        Assert.Equal(20, stats.LastSentHeight);
        Assert.Equal(25, stats.LastSentValue);
    }
}
