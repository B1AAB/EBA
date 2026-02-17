namespace EBA.Blockchains.Bitcoin.GraphModel;

public enum NodeKind
{
    Coinbase = 0,
    Script = 1,
    Block = 2,
    Tx = 3
}

public record EdgeKind(NodeKind Source, NodeKind Target, RelationType Relation)
{
    public override string ToString()
    {
        return $"{Source}-[{Relation}]->{Target}";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Target, Relation);
    }
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