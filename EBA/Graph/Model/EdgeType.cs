namespace EBA.Graph.Model;

public enum EdgeType
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
