namespace BC2G.Graph.Model;

// TODO: try merging the following two types, separated seems redundant. 

public enum EdgeLabel
{
    C2TMinting = 0,
    C2SMinting = 1,
    T2TTransfer = 2,
    T2TFee = 3,
    S2STransfer = 4,
    S2SFee = 5,
    S2BRedeems = 6,
    B2SCredits = 7,
    T2BRedeems = 8,
    B2TConfirms = 9
}

public enum EdgeType
{
    Mints = 0,
    Transfers = 1,
    Fee = 2,
    Redeems = 3,
    Confirms = 4,
    Credits = 5
}
