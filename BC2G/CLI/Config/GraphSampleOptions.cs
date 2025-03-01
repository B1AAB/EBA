namespace BC2G.CLI.Config;

public enum GraphSampleMode
{
    SubGraphOnly,
    GraphRndEdgePair
}

public enum CoinbaseSelectionMode
{
    CoinbaseOnly,
    IncludeCoinbase,
    ExcludeCoinbase
}

public enum PathSearchAlgorith
{
    // Breadth-first Search
    BFS,

    // Depth-first Search
    DFS
}

public class GraphSampleOptions
{
    public int Count { init; get; }
    public int Hops { init; get; }
    public GraphSampleMode Mode { init; get; } = GraphSampleMode.SubGraphOnly;
    public CoinbaseSelectionMode CoinbaseMode { init; get; } = CoinbaseSelectionMode.IncludeCoinbase;
    public PathSearchAlgorith PathSearchAlgorith { init; get; } = PathSearchAlgorith.DFS;
    public int MinNodeCount { init; get; } = 3;
    public int MaxNodeCount { init; get; } = 500;
    public int MinEdgeCount { init; get; } = 3;
    public int MaxEdgeCount { init; get; } = 10000;
    public int MaxAttempts { init; get; } = 25;
    public int MaxNodeFetchFromNeighbor { init; get; } = 500;
    public int MaxEdgesFetchFromNeighbor { init; get; } = 10000;


    public double RootNodeSelectProb
    {
        init
        {
            if (value < 0 || value > 1)
                _rootNodeSelectProb = 1;
            else
                _rootNodeSelectProb = value;
        }
        get { return _rootNodeSelectProb; }
    }
    private double _rootNodeSelectProb = 0.3;
}
