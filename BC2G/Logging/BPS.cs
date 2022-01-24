namespace BC2G.Logging
{
    // if new values are added, or the index of items is 
    // changed, _messages array in Logger needs to be updated
    // to reflect changes.

    /// <summary>
    /// Block Processing Status.
    /// </summary>
    public enum BPS : byte
    {
        StartBlock = 0,
        GetBlockHash = 1,
        GetBlockHashDone = 2,
        GetBlockHashCancelled = 3,
        GetBlock = 4,
        GetBlockDone = 5,
        GetBlockCancelled = 6,
        ProcessTransactions = 7,
        ProcessTransactionsStatus = 8,
        ProcessTransactionsDone = 9,
        ProcessTransactionsCancelled = 10,
        Serialize = 11,
        SerializeDone = 12,
        SerializeCancelled = 13,
        Successful = 14,
        Cancelled = 15,
        Cancelling = 16
    }
}
