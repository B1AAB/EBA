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
}
