namespace BC2G.Logging
{
    // if new values are added, or the index of items is 
    // changed, _messages array in Logger needs to be updated
    // to reflect changes.

    public enum BlockProcessStatus : byte
    {
        Cancelling = 0,
        GetBlockHash = 1,
        GetBlock = 2,
        ProcessTransactions = 3,
        Serialize = 4
    }
}
