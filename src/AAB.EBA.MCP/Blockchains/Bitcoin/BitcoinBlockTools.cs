using AAB.EBA.MCP.Infrastructure;
using System.Text.Json;

namespace AAB.EBA.MCP.Blockchains.Bitcoin;

[McpServerToolType, Description(
    "Tools for getting information about bitcoin blocks")]
public class BitcoinBlockTools(BitcoinMcpService mcpService)
{
    private readonly BitcoinMcpService _mcpService = mcpService;

    [McpServerTool, Description("Get the latest bitcoin block height in this service (database cutoff point)")]
    public async Task<string> GetLatestBlock()
    {
        var height = await _mcpService.GetLatestBlockHeightAsync();        
        return height.ToString() ?? "No blocks found in the database.";
    }

    [McpServerTool, Description("Get information about a bitcoin block by its height.")]
    public async Task<string> GetBlockInfo(
        [Description("The block height to get info for")] long height,
        [Description("Whether to include the median time of the block")] bool includeMedianTime = false,
        [Description("Whether to include the transaction count of the block")] bool includeTxCount = false,
        [Description("Whether to include the minted coins of the block")] bool includeMintedCoins = false,
        [Description("Whether to include the burned coins of the block")] bool includeBurnedCoins = false,
        [Description("Whether to include the total supply of the block")] bool includeTotalSupply = false)
    {
        var blockNode = await _mcpService.GetBlockByHeightAsync(height);
        if (blockNode == null)
            return $"Block at height {height} not found.";

        var responsePayload = new Dictionary<string, object>();

        if (includeMedianTime)
            responsePayload["MedianTime"] = blockNode.BlockMetadata.MedianTime;

        if (includeTxCount)
            responsePayload["TxCount"] = blockNode.BlockMetadata.TransactionsCount;

        if (includeMintedCoins)
            responsePayload["MintedCoins"] = blockNode.BlockMetadata.MintedBitcoins;

        if (includeBurnedCoins)
            responsePayload["BurnedCoins"] = blockNode.BlockMetadata.ProvablyUnspendableBitcoins;

        if (includeTotalSupply)
            responsePayload["TotalSupply"] = blockNode.BlockMetadata.TotalSupply?.ToString() ?? "";

        return JsonSerializer.Serialize(responsePayload, McpJsonOptions.Default);
    }
}
