namespace EBA.CLI.Config;

public enum GraphTraversal
{
    // path search algorithm
    // traverse the graph using the given algorithm 
    // deterministic sampling algorithm
    // stops when a criteria is met (e.g., max number of nodes or edges sampled)
    // Breadth-first Search
    //BFS,

    // path search algorithm
    // traverse the graph using the given algorithm 
    // deterministic sampling algorithm
    // stops when a criteria is met (e.g., max number of nodes or edges sampled)
    // Depth-first Search
    //DFS,

    // sampling algorithm 
    // non-deterministic sampling algorithm
    // Forest Fire sampling
    FFS
}

public class BitcoinGraphSampleOptions
{
    public int Count { init; get; }
    public GraphTraversal TraversalAlgorithm { init; get; } = GraphTraversal.FFS;
    public int MinNodeCount { init; get; } = 500;
    public int MaxNodeCount { init; get; } = 1000;
    public int MinEdgeCount { init; get; } = 499;
    public int MaxEdgeCount { init; get; } = 10000;
    public int MaxAttempts { init; get; } = 25;


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
    //public string LabelFilters { init; get; } = $"{ScriptNodeStrategy.Label}|{BlockNodeStrategy.Label}";

    public BitcoinForestFireOptions ForestFireOptions { init; get;  } = new BitcoinForestFireOptions();
}
