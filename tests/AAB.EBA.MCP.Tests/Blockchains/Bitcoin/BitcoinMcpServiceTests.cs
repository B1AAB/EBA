using AAB.EBA.GraphDb.Tests;
using AAB.EBA.MCP.Blockchains.Bitcoin;

namespace AAB.EBA.MCP.Tests.Blockchains.Bitcoin;

public class BitcoinMcpServiceTests
{
    private readonly BitcoinMcpService _mcpService;

    public BitcoinMcpServiceTests()
    {
        var graph = BitcoinGraphScenarios.GetCommunity1();
        var fakeDb = new FakeGraphDb(graph);
        _mcpService = new BitcoinMcpService(fakeDb);
    }

    [Fact]
    public async Task GetScriptBalance_WhenNodeExists_ReturnsMappedScriptNode()
    {
        // Act
        var balance = await _mcpService.GetScriptBalanceAsync("hash1", ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(balance);
        Assert.Equal(50, balance);
    }

    [Fact]
    public async Task GetScriptByAddress_WhenValidAddress_ReturnScriptNode()
    {
        // Act
        var scriptNode = await _mcpService.GetScriptByAddressAsync("add1", ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(scriptNode);
        Assert.Equal("add1", scriptNode.Address);
        Assert.Equal("hash1", scriptNode.SHA256Hash);
    }

    [Fact]
    public async Task GetScriptBySHA256_WhenValidSHA256_ReturnScriptNode()
    {
        // Act
        var scriptNode = await _mcpService.GetScriptBySHA256Async("hash1", ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(scriptNode);
        Assert.Equal("add1", scriptNode.Address);
        Assert.Equal("hash1", scriptNode.SHA256Hash);
    }

    [Fact]
    public async Task GetScriptTxSummaryStats_WhenValidSHA_ReturnStats()
    {
        // Act
        var stats = await _mcpService.GetScriptTxSummaryStatsAsync("hash1", ct: TestContext.Current.CancellationToken);

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
