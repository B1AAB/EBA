namespace BC2G.CLI.Config;

public enum GraphSampleMode
{
    SubGraphOnly,
    GraphRndEdgePair
}

public class GraphSampleOptions
{
    public int Count { init; get; }
    public int Hops { init; get; }
    public GraphSampleMode Mode { init; get; } = GraphSampleMode.SubGraphOnly;
    public int MinNodeCount { init; get; } = 3;
    public int MaxNodeCount { init; get; } = 200;
    public int MinEdgeCount { init; get; } = 3;
    public int MaxEdgeCount { init; get; } = 200;
    public int MaxAttempts { init; get; } = 3;
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
    private double _rootNodeSelectProb = 0.1;
}
