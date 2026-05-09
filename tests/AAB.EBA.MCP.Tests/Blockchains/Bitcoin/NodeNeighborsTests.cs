using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.GraphDb;
using AAB.EBA.GraphDb.Tests.Neo4j;
using AAB.EBA.MCP.Blockchains.Bitcoin;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace AAB.EBA.MCP.Tests.Blockchains.Bitcoin;

public class NodeNeighborsTests : IClassFixture<DbFixture>
{
    private readonly Neo4jDb _db;
    private readonly DbFixture _fixture;
    private readonly BitcoinTxTools _txTools;
    private readonly BitcoinScriptTools _scriptTools;

    public NodeNeighborsTests(DbFixture fixture)
    {
        _fixture = fixture;

        var options = new Options { Neo4j = _fixture.Neo4jOptions };

        var logger = NullLogger<Neo4jDb>.Instance;
        _db = new Neo4jDb(options, logger);

        // TODO: this is a hack. Need it to access strategy factory from the service
        var bitcoinGraphDb = new BitcoinNeo4jDb(options, NullLogger<Neo4jDb<BitcoinGraph>>.Instance);

        var mcpService = new BitcoinMcpService(_db, bitcoinGraphDb);

        _txTools = new BitcoinTxTools(mcpService);
        _scriptTools = new BitcoinScriptTools(mcpService);
    }

    [Fact]
    public async Task GetTxNeighborsByTxid_WhenTxExists_ReturnNeighbors()
    {
        // Act
        var response = await _txTools.GetTxNeighbors(txid: "tx1");

        // Assert
        Assert.NotNull(response);
        Assert.DoesNotContain("Did not find", response);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        // SpentUTxOs assertions
        Assert.True(root.TryGetProperty("SpentUTxOs", out var spentUtxos));
        Assert.Equal(JsonValueKind.Array, spentUtxos.ValueKind);
        Assert.Equal(1, spentUtxos.GetArrayLength());

        var spentTxo = spentUtxos[0];
        Assert.Equal("hash2", spentTxo.GetProperty("ScriptSHA256").GetString());
        Assert.Equal(25m, spentTxo.GetProperty("Value").GetDecimal());
        Assert.Equal("tx_created_1", spentTxo.GetProperty("TxCreatingTxo_Txid").GetString());
        Assert.Equal(1, spentTxo.GetProperty("TxCreatingTxo_Vout").GetInt32());
        Assert.Equal(10, spentTxo.GetProperty("CreationHeight").GetInt64());
        Assert.Equal(10, spentTxo.GetProperty("AgeAtSpending").GetInt64());

        // CreatedUTxOs assertions
        Assert.True(root.TryGetProperty("CreatedUTxOs", out var createdUtxos));
        Assert.Equal(JsonValueKind.Array, createdUtxos.ValueKind);
        Assert.Equal(1, createdUtxos.GetArrayLength());

        var createdTxo = createdUtxos[0];
        Assert.Equal("hash1", createdTxo.GetProperty("ScriptSHA256").GetString());
        Assert.Equal(25m, createdTxo.GetProperty("Value").GetDecimal());
        Assert.Equal(20, createdTxo.GetProperty("CreationHeight").GetInt64());
        Assert.Equal(50, createdTxo.GetProperty("SpentHeight").GetInt64());
    }

    [Fact]
    public async Task GetScriptNeighborsBySHA_WhenScriptExists_ReturnNeighbors()
    {
        // Act
        var response = await _scriptTools.GetScriptNeighbors(sha: "hash1");

        // Assert
        Assert.NotNull(response);
        Assert.DoesNotContain("Either address or SHA256 hash must be provided.", response);
        Assert.DoesNotContain("Script not found", response);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        // RedeemedIn assertions
        Assert.True(root.TryGetProperty("RedeemedIn", out var redeemedIn));
        Assert.Equal(JsonValueKind.Array, redeemedIn.ValueKind);
        Assert.Equal(1, redeemedIn.GetArrayLength());

        var redeemed = redeemedIn[0];
        Assert.Equal(25m, redeemed.GetProperty("Value").GetDecimal());
        Assert.Equal("tx3", redeemed.GetProperty("Txid").GetString());
        Assert.Equal(50, redeemed.GetProperty("Height").GetInt64());

        // RewardedIn assertions
        Assert.True(root.TryGetProperty("RewardedIn", out var rewardedIn));
        Assert.Equal(JsonValueKind.Array, rewardedIn.ValueKind);
        Assert.Equal(2, rewardedIn.GetArrayLength());

        var rewarded0 = rewardedIn[0];
        Assert.Equal(25m, rewarded0.GetProperty("Value").GetDecimal());
        Assert.Equal("tx1", rewarded0.GetProperty("Txid").GetString());
        Assert.Equal(20, rewarded0.GetProperty("Height").GetInt64());

        var rewarded1 = rewardedIn[1];
        Assert.Equal(50m, rewarded1.GetProperty("Value").GetDecimal());
        Assert.Equal("tx2", rewarded1.GetProperty("Txid").GetString());
        Assert.Equal(30, rewarded1.GetProperty("Height").GetInt64());
    }
}
