namespace AAB.EBA.MCP.Blockchains.Bitcoin;

public record ScriptTxSummaryStats(
    int TxCount,
    long TotalReceived,
    long TotalSent,
    long FirstReceivedHeight,
    long FirstReceivedValue,
    long FirstSentHeight,
    long FirstSentValue,
    long LastReceivedHeight,
    long LastReceivedValue,
    long LastSentHeight,
    long LastSentValue
);
