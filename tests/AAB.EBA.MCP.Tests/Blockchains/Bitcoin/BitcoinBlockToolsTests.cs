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
        var fakeDb = new FakeGraphDb(BitcoinGraphScenarios.GetCommunity1());

        _mcpService = new BitcoinMcpService(fakeDb);
        _tools = new BitcoinBlockTools(_mcpService);
    }

    [Fact]
    public async Task GetLatestBlock_WhenBlocksExist_ReturnsLatestBlockHeight()
    {
        // Act
        var result = await _tools.GetLatestBlock();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("50", result);
    }

    [Fact]
    public async Task GetBlockInfo_WhenBlockExists_ReturnsCorrectJsonPayload()
    {
        // Act
        var jsonResult = await _tools.GetBlockInfo(height: 10, includeMedianTime: true);

        // Assert
        Assert.NotNull(jsonResult);
        Assert.NotEqual("Block at height 10 not found.", jsonResult);

        var node = JsonNode.Parse(jsonResult);
        Assert.NotNull(node);

        var medianTimeNode = node["MedianTime"];
        Assert.NotNull(medianTimeNode);
        Assert.Equal(1L, (long)medianTimeNode);
    }

    [Fact]
    public async Task GetBlockInfo_GetMarketAndOhlcv_ReturnValidValues()
    {
        // Act
        var jsonResult = await _tools.GetBlockInfo(height: 50, includeMarketCap: true, includeOHLCV: true);

        // Assert
        Assert.NotNull(jsonResult);
        Assert.NotEqual("Block at height 50 not found.", jsonResult);

        var node = JsonNode.Parse(jsonResult);
        Assert.NotNull(node);

        var marketCapNode = node["MarketCap"];
        Assert.NotNull(marketCapNode);
        Assert.Equal("0.000007", marketCapNode.ToString());


        var ohlcvNode = node["OHLCV"];
        Assert.NotNull(ohlcvNode);

        var ohlcv = JsonNode.Parse(ohlcvNode.ToString());
        Assert.NotNull(ohlcv);

        Assert.Equal("5", ohlcv["Open"]?.ToString());
        Assert.Equal("9", ohlcv["High"]?.ToString());
        Assert.Equal("1", ohlcv["Low"]?.ToString());
        Assert.Equal("8", ohlcv["Close"]?.ToString());
    }
}
