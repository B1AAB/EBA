using AAB.EBA.GraphDb.Tests;
using AAB.EBA.MCP.Blockchains.Bitcoin;
using System.Text.Json;

namespace AAB.EBA.MCP.Tests.Blockchains.Bitcoin;

public class BitcoinScriptToolsTests
{
    private readonly BitcoinScriptTools _tools;

    public BitcoinScriptToolsTests()
    {
        var graph = BitcoinGraphScenarios.GetCommunity1();
        var fakeDb = new FakeGraphDb(graph);
        var mcpService = new BitcoinMcpService(fakeDb);

        _tools = new BitcoinScriptTools(mcpService);
    }

    [Fact]
    public async Task GetScript_WhenAddressProvidedAndScriptExists_ReturnsCorrectJsonPayload()
    {
        // Act
        var jsonResult = await _tools.GetScript(address: "add1");

        // Assert
        Assert.NotNull(jsonResult);
        Assert.DoesNotContain("Did not find", jsonResult);

        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal("PubKey", root.GetProperty("ScriptType").GetString());
        Assert.Equal("hash1", root.GetProperty("SHA256Hash").GetString());

        Assert.False(root.TryGetProperty("Address", out _));
    }

    [Fact]
    public async Task GetScript_WhenScriptProvidedButScriptDoesNotExist_ReturnsErrorMessage()
    {
        // Act
        var result = await _tools.GetScript("fake_address");

        // Assert
        Assert.Equal("Did not find a script with given address: fake_address", result);
    }

    [Fact]
    public async Task GetScript_WhenScriptHashGivenAndIncludeBalanceTrue_ReturnsJsonWithBalance()
    {
        // Act
        // 'hash1' has two T2S (Credits) edges seeded:
        // 1. Value = 25 (Spent at height 50)
        // 2. Value = 50 (Unspent / SpentHeight = long.MaxValue)
        var result = await _tools.GetScript(sha: "hash1", includeBalance: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Did not find", result);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal("PubKey", root.GetProperty("ScriptType").GetString());
        Assert.Equal("hash1", root.GetProperty("SHA256Hash").GetString());

        Assert.True(
            root.TryGetProperty("Balance", out var balanceElement), 
            "JSON payload is missing the 'Balance' property.");

        Assert.Equal(50, balanceElement.GetInt64());
    }
}
