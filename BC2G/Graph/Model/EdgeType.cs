namespace BC2G.Graph.Model;

public enum EdgeLabel
{
    C2TGeneration = 0,
    C2SGeneration = 1,
    T2TTransfer = 2,
    T2TFee = 3,
    S2STransfer = 4,
    S2SFee = 5
}

public enum EdgeType
{
    Generation = 0,
    Transfer = 1,
    Fee = 2
}
