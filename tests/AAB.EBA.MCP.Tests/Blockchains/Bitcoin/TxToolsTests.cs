using AAB.EBA.GraphDb.Tests;
using AAB.EBA.MCP.Blockchains.Bitcoin;
using System.Text.Json;

namespace AAB.EBA.MCP.Tests.Blockchains.Bitcoin;

public class TxToolsTests
{
    private readonly BitcoinTxTools _tools;

    public TxToolsTests()
    {
        var graph = BitcoinGraphScenarios.GetCommunity1();
        var fakeDb = new FakeGraphDb(graph);
        var mcpService = new BitcoinMcpService(fakeDb);

        _tools = new BitcoinTxTools(mcpService);
    }

    [Fact]
    public async Task GetTx_WhenTxIdProvidedAndTxExists_ReturnsCorrectJsonPayload()
    {
        // Act
        var jsonResult = await _tools.GetTxSummary(txid: "tx1");

        // Assert
        Assert.NotNull(jsonResult);

        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal("10", root.GetProperty("Height").ToString());
        Assert.Equal("25", root.GetProperty("SumOfUTxOSpentInTx(InValue)").ToString());
        Assert.Equal("0", root.GetProperty("SumOfUTxOOfCoinbaseOutputSpentInTx").ToString());
        Assert.Equal("25", root.GetProperty("SumOfCreatedUTxOInTx(OutValue)").ToString());
        Assert.Equal("1", root.GetProperty("TotalInputScripts").ToString());
        Assert.Equal("1", root.GetProperty("TotalOutputScripts").ToString());
        Assert.Equal("1", root.GetProperty("UniqueInputScripts").ToString());
        Assert.Equal("1", root.GetProperty("UniqueOutputScripts").ToString());
    }
}
