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


public record TxDTO(
    long Height,
    decimal Fee,
    decimal InValue,
    decimal OutValue,
    decimal InValueGenerated,
    int TotalInputScripts,
    int TotalOutputScripts,
    int UniqueInputScripts,
    int UniqueOutputScripts,
    long MinInputAge,
    long MaxOutputAge,
    long MaxOutputSpentHeight,
    long MinOutputSpentHeight,
    decimal OutputValueSpent // how much of the created output value (UTxO) is spent in a subsequent Tx.
);
