using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.MCP.Infrastructure;
using System.Text.Json;

namespace AAB.EBA.MCP.Blockchains.Bitcoin;

[McpServerToolType, Description(
    "Tools for getting information about bitcoin transactions.")]
public class BitcoinTxTools(BitcoinMcpService mcpService)
{
    private readonly BitcoinMcpService _mcpService = mcpService;

    [McpServerTool, Description(
        "Given a bitcoin Tx ID returns the following information: " +
        "* Height that confirms (contains) the Tx; " +
        "* Sum of Fee paid; " +
        "* Sum value of UTxO spent; " +
        "* Sum Value of UTxO created; " +
        "* Sum value of UTxO spent that were created in a Coinbase Tx " +
        "(i.e., minted coins), will be less than or equal to sum value of UTxO spent; " +
        "* Total number of input scripts; " +
        "* Total number of output scripts; " +
        "* Total number of unique input scripts; " +
        "* Total number of unique output scripts; " +
        "* Min input age (age of spent UTxO or coin dormancy); " +
        "* Max input age;" +
        "* Min output spent height (if the created UTxO in Tx is spent (until the cut-off block height), the min height of all created UTxO); " +
        "* Max output spent height; " +
        "* Sum value of created UTxO that is spent. ")]
    public async Task<string> GetTxSummary(
        [Description("Id of the Bitcoin transaction")] string txid)
    {
        var txNodeDTO = await _mcpService.GetTxSummaryByTxidAsync(txid);

        if (txNodeDTO == null)
            return $"Did not find a transaction with given id: {txid}";

        var responsePayload = new Dictionary<string, object>
        {
            { "Height", txNodeDTO.Height },
            { "Fee", txNodeDTO.Fee },
            { "SumOfUTxOSpentInTx(InValue)", txNodeDTO.InValue },
            { "SumOfUTxOOfCoinbaseOutputSpentInTx", txNodeDTO.InValueGenerated },
            { "SumOfCreatedUTxOInTx(OutValue)", txNodeDTO.OutValue },
            { "TotalInputScripts", txNodeDTO.TotalInputScripts },
            { "TotalOutputScripts", txNodeDTO.TotalOutputScripts },
            { "UniqueInputScripts", txNodeDTO.UniqueInputScripts },
            { "UniqueOutputScripts", txNodeDTO.UniqueOutputScripts },
            { "MinInputAge", txNodeDTO.MinInputAge },
            { "MaxOutputAge", txNodeDTO.MaxOutputAge },
            { "MaxOutputSpentHeight", txNodeDTO.MaxOutputSpentHeight },
            { "MinOutputSpentHeight", txNodeDTO.MinOutputSpentHeight },
            { "OutputValueSpent", txNodeDTO.OutputValueSpent }
        };

        return JsonSerializer.Serialize(responsePayload, McpJsonOptions.Default);
    }


    [McpServerTool, Description(
        "Given a bitcoin Tx ID returns the following information about its neighboring transactions (limited to max 10 spent and max 10 created Txo): " +
        "* For each spent UTxO (i.e., Tx input):" +
        "  ** Script address (or SHA256 hash if address is not available), " +
        "  ** value, " +
        "  ** Txid and vout of the UTxO, " +
        "  ** height that created the UTxO, and " +
        "  ** age of the UTxO at spending (i.e., difference between height that created the UTxO and height that spends it); " +
        "* For each created UTxO (i.e., Tx output): " +
        "  ** Script address (or SHA256 hash if address is not available), " +
        "  ** value, " +
        "  ** height that created the UTxO, and " +
        "  ** if the UTxO is spent as of the cut-off block height, the height that spent it. ")]
    public async Task<string> GetTxNeighbors(
        [Description("Id of the Bitcoin transaction")] string txid, 
        [Description("Maximum number of spent UTxOs to return (default 10--do not increase unless asked specifically)")] int maxSpentTxo = 10, 
        [Description("Maximum number of created UTxOs to return (default 10--do not increase unless asked specifically)")] int maxCreatedTxo = 10)
    {
        // This gets neighbors at 0 hop, so immediate neighbors only
        var neighborhood = await _mcpService.GetTxNodeNeighborsAsync(txid);

        if (neighborhood.NodeCount == 0 || neighborhood.EdgeCount == 0)
            return $"Tx not found: {txid}";        

        var responsePayload = new Dictionary<string, object>();

        var counter = 0;
        var spentUTxOs = new List<Dictionary<string, object>>();
        foreach (var e in neighborhood.EdgesByType[S2TEdge.Kind])
        {
            if (++counter == maxSpentTxo)
                break;

            neighborhood.TryGetNode(e.Source.Id, out var v);
            if (v == null) continue;

            var scriptNode = (ScriptNode)v;
            var edge = (S2TEdge)e;

            var utxo = new Dictionary<string, object>
            {
                ["Value"] = edge.Value,
                ["TxCreatingTxo_Txid"] = edge.Txid,
                ["TxCreatingTxo_Vout"] = edge.Vout,
                ["CreationHeight"] = edge.CreationHeight,
                ["AgeAtSpending"] = edge.SpentHeight - edge.CreationHeight
            };

            if (string.IsNullOrEmpty(scriptNode.Address))
                utxo["ScriptSHA256"] = scriptNode.SHA256Hash;
            else
                utxo["ScriptAddress"] = scriptNode.Address;

            spentUTxOs.Add(utxo);
        }

        counter = 0;
        var createdUTxOs = new List<Dictionary<string, object>>();
        foreach (var e in neighborhood.EdgesByType[T2SEdge.Kind])
        {
            if (++counter == maxCreatedTxo)
                break;

            neighborhood.TryGetNode(e.Target.Id, out var v);
            if (v == null) continue;

            var scriptNode = (ScriptNode)v;
            var edge = (T2SEdge)e;

            var txo = new Dictionary<string, object>
            {
                ["ScriptSHA256"] = scriptNode.SHA256Hash,
                ["Value"] = edge.Value,
                ["CreationHeight"] = edge.CreationHeight,
                ["SpentHeight"] = edge.SpentHeight == long.MaxValue ? "Not spent as of the cutoff height" : edge.SpentHeight
            };

            if (string.IsNullOrEmpty(scriptNode.Address))
                txo["ScriptSHA256"] = scriptNode.SHA256Hash;
            else
                txo["ScriptAddress"] = scriptNode.Address;

            createdUTxOs.Add(txo);
        }

        responsePayload.Add("SpentUTxOs", spentUTxOs);
        responsePayload.Add("CreatedUTxOs", createdUTxOs);

        return JsonSerializer.Serialize(responsePayload, McpJsonOptions.Default);
    }
}
