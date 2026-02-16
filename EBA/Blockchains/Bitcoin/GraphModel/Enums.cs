namespace EBA.Blockchains.Bitcoin.GraphModel;

public enum NodeKind
{
    Coinbase,
    Script,
    Block,
    Tx
}

public enum RelationType
{
    Mints = 0,
    Transfers = 1,
    Fee = 2,
    Redeems = 3,
    Confirms = 4,
    Credits = 5,

    /// <summary>
    /// The difference between this and Mints is that, 
    /// this includes both fee and minted coins.
    /// </summary>
    Rewards = 6,

    Contains = 7
}