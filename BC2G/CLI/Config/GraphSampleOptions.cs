using BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

namespace BC2G.CLI.Config;

public enum GraphSampleMode
{
    /// <summary>
    /// Graph is a single connected component.
    /// </summary>
    ConnectedGraph,

    /// <summary>
    /// Graph is a forest of connected components, i.e., a collection of disjoint graphs.
    /// </summary>
    ConnectedGraphAndForest
}

public enum CoinbaseSelectionMode
{
    CoinbaseOnly,
    IncludeCoinbase,
    ExcludeCoinbase
}

public enum SamplingAlgorithm
{
    // path search algorithm
    // traverse the graph using the given algorithm 
    // deterministic sampling algorithm
    // stops when a criteria is met (e.g., max number of nodes or edges sampled)
    // Breadth-first Search
    BFS,

    // path search algorithm
    // traverse the graph using the given algorithm 
    // deterministic sampling algorithm
    // stops when a criteria is met (e.g., max number of nodes or edges sampled)
    // Depth-first Search
    DFS,

    // sampling algorithm 
    // non-deterministic sampling algorithm
    // Forest Fire sampling
    FFS
}

public enum EdgeTypes
{
    S2S,
    ALL
}

public class GraphSampleOptions
{
    public int Count { init; get; }
    public int Hops { init; get; }
    public GraphSampleMode Mode { init; get; } = GraphSampleMode.ConnectedGraphAndForest;
    public CoinbaseSelectionMode CoinbaseMode { init; get; } = CoinbaseSelectionMode.ExcludeCoinbase;
    public SamplingAlgorithm Algorithm { init; get; } = SamplingAlgorithm.DFS;
    public EdgeTypes[] IncludeEdgeTypes { init; get; } = [EdgeTypes.S2S];
    public int MinNodeCount { init; get; } = 500;
    public int MaxNodeCount { init; get; } = 1000;
    public int MinEdgeCount { init; get; } = 499;
    public int MaxEdgeCount { init; get; } = 10000;
    public int MaxAttempts { init; get; } = 25;

    // TODO: the following two are confusing and not clear how they 
    // support/match the above max/min node/edge counts. 
    // Also the value of these configs should be far more than the 
    // above (see where they are used in the queries).
    // Try to consilidate the following and the above into more 
    // intuitive settings. 
    public int MaxNodeFetchFromNeighbor { init; get; } = 10000;
    public int MaxEdgesFetchFromNeighbor { init; get; } = 500000;

    public bool SerializeEdges { init; get; } = false;
    public bool SerializeFeatureVectors { init; get; } = true;

    public int ForestFireNodeSamplingCountAtRoot { init; get; } = 100;
    public int ForestFireMaxHops { init; get; } = 4;
    public int ForestFireQueryLimit { init; get; } = 1000;
    public double ForestFireNodeCountReductionFactorByHop { init; get; } = 4.0;


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

    // you can combine multiple like this: {ScriptNodeStrategy.Labels}|{TxNodeStrategy.Labels}|{BlockNodeStrategy.Labels}
    // blacklist with -
    // whitelist with +
    // termination filter /
    // > end node filter
    // +ScriptNodeStrategy.Labels|-TxNodeStrategy.Labels|>BlockNodeStrategy.Labels
    // more details: https://neo4j.com/labs/apoc/4.1/overview/apoc.path/apoc.path.spanningTree/#expand-spanning-tree-label-filters
    public string LabelFilters { init; get; } = ScriptNodeStrategy.Labels; //$"{ScriptNodeStrategy.Labels}|{BlockNodeStrategy.Labels}"; //
}
