using AAB.EBA.MCP.Infrastructure;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using System.Text.Json;

namespace AAB.EBA.MCP.Blockchains.Bitcoin;

[McpServerToolType, Description(
    "Tools for getting information about bitcoin addresses and scripts.")]
public class BitcoinScriptTools(BitcoinMcpService mcpService)
{
    private readonly BitcoinMcpService _mcpService = mcpService;

    [McpServerTool, Description(
        "Get a bitcoin script/address by its address or SHA256 hash, " +
        "returns the script type and SHA256 hash and optionally the balance.")]
    public async Task<string> GetScript(
        [Description("The address of the bitcoin script")] string? address = null,
        [Description("The SHA256 hash of the bitcoin script")] string? sha = null,
        [Description("[Optional, default false] Whether to include the balance of the script")] bool includeBalance = false,
        [Description("[Optional, default latest] Block height to get the balance at; if includeBalance is true.")] long? block = null)
    {
        ScriptNode? scriptNode;

        if (address != null)
            scriptNode = await _mcpService.GetScriptByAddressAsync(address);
        else if (sha != null)
            scriptNode = await _mcpService.GetScriptBySHA256Async(sha);
        else
            return "Either address or SHA256 hash must be provided.";

        if (scriptNode == null)
            return $"Did not find a script with given address: {address}";

        var responsePayload = new Dictionary<string, object>
        {
            { "ScriptType", scriptNode.ScriptType.ToString() },
            { "SHA256Hash", scriptNode.SHA256Hash }
        };

        if (includeBalance)
        {
            var balance = await _mcpService.GetScriptBalanceAsync(scriptNode.SHA256Hash, block ?? long.MaxValue);
            responsePayload.Add("Balance", balance);
        }

        return JsonSerializer.Serialize(responsePayload, McpJsonOptions.Default);
    }

    [McpServerTool, Description("Get detailed info about a script's transactions.")]
    public async Task<string> GetScriptTxInfo(
        [Description("The address of the bitcoin script")] string? address = null,
        [Description("The SHA256 hash of the bitcoin script")] string? sha = null)
    {
        ScriptNode? scriptNode;

        if (address != null)
            scriptNode = await _mcpService.GetScriptByAddressAsync(address);
        else if (sha != null)
            scriptNode = await _mcpService.GetScriptBySHA256Async(sha);
        else
            return "Either address or SHA256 hash must be provided.";

        if (scriptNode == null)
            return $"Did not find a script with given address: {address}";

        var stats = await _mcpService.GetScriptTxSummaryStatsAsync(sha);

        if (stats == null)
            return $"No transactions found for script with SHA256 hash: {sha}";

        return JsonSerializer.Serialize(stats, McpJsonOptions.Default);
    }

    [McpServerTool, Description(
        "For a Bitcoin script returns the following: " +
        "* Transactions where the script was referenced in the input (i.e., redeeming UTxO) with value, txid, and block height; " +
        "* Transactions where the script was referenced in the output (i.e., rewarding UTxO) with value, txid, and block height. ")]
    public async Task<string> GetScriptNeighbors(
        [Description("The SHA256 hash of the bitcoin script (optional; require SHA256 or address)")] string? sha = null,
        [Description("The address of the bitcoin script (optional; require SHA256 or address)")] string? address = null,
        [Description("Maximum number of spent UTxOs to return (default 10--do not increase unless asked specifically)")] int maxRedeemedIn = 10,
        [Description("Maximum number of created UTxOs to return (default 10--do not increase unless asked specifically)")] int maxRewardedIn = 10)
    {
        if (string.IsNullOrEmpty(sha) && string.IsNullOrEmpty(address))
            return "Either address or SHA256 hash must be provided.";

        // This gets neighbors at 0 hop, so immediate neighbors only
        var neighborhood = await _mcpService.GetScriptNodeNeighbors(sha: sha, address: address);

        if (neighborhood.NodeCount == 0 || neighborhood.EdgeCount == 0)
            return $"Script not found: {sha ?? address}";

        var counter = 0;
        var redeemedIn = new List<Dictionary<string, object>>();
        foreach (var e in neighborhood.EdgesByType[S2TEdge.Kind])
        {
            if (++counter == maxRedeemedIn)
                break;

            neighborhood.TryGetNode(e.Target.Id, out var v);
            if (v == null) continue;

            var txNode = (TxNode)v;
            var edge = (S2TEdge)e;

            redeemedIn.Add(new Dictionary<string, object>
            {
                ["Value"] = edge.Value,
                ["Txid"] = txNode.Txid,
                ["Height"] = edge.Height
            });
        }

        counter = 0;
        var rewardedIn = new List<Dictionary<string, object>>();
        foreach (var e in neighborhood.EdgesByType[T2SEdge.Kind])
        {
            if (++counter == maxRewardedIn)
                break;

            neighborhood.TryGetNode(e.Source.Id, out var v);
            if (v == null) continue;

            var txNode = (TxNode)v;
            var edge = (T2SEdge)e;

            rewardedIn.Add(new Dictionary<string, object>
            {
                ["Value"] = edge.Value,
                ["Txid"] = txNode.Txid,
                ["Height"] = edge.Height
            });
        }

        var responsePayload = new Dictionary<string, object>
        {
            ["RedeemedIn"] = redeemedIn,
            ["RewardedIn"] = rewardedIn
        };

        return JsonSerializer.Serialize(responsePayload, McpJsonOptions.Default);
    }
}
