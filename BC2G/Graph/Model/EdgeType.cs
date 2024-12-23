namespace BC2G.Graph.Model;

public enum EdgeLabel
{
    C2TMinting = 0,
    C2SMinting = 1,
    T2TTransfer = 2,
    T2TFee = 3,
    S2STransfer = 4,
    S2SFee = 5
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
