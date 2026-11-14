namespace AAB.EBA.CLI.Config;

public class BitcoinPanoramaSamplingAlgorithmOptions
{
    [JsonPropertyName("maxHops")]
    public int MaxHops { init; get; } = 2;

    [JsonPropertyName("queryLimit")]
    public int QueryLimit { init; get; } = 1000;

    [JsonPropertyName("neighborhoodSamplePercentagePerHop")]
    public double NeighborhoodSamplePercentagePerHop { init; get; } = 10.0;

    /// <summary>
    /// Use a fixed seed when sampling neighbors. Database root selection and query
    /// ordering are not controlled by this option.
    /// </summary>
    [JsonPropertyName("deterministicSampling")]
    public bool DeterministicSampling { init; get; } = false;

    /// <summary>
    /// Sets the percentage of the immediate neighbors of the root node
    /// to be included in the sampled community.
    /// The value should be between 0 and 100, 
    /// where 100 means all immediate neighbors of the root node will be included.
    /// </summary>
    [JsonPropertyName("nodeSamplingPercentageAtRoot")]
    public double NodeSamplingPercentageAtRoot
    {
        init
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(NodeSamplingPercentageAtRoot),
                    "Value must be between 0 and 100.");
            }
            _nodeSamplingPercentageAtOtherHops = value;
        }
        get
        {
            return _nodeSamplingPercentageAtOtherHops;
        }
    }
    private double _nodeSamplingPercentageAtOtherHops = 100;

    /// <summary>
    /// This could be used to exclude high degree nodes, 
    /// such as mixer nodes or nodes belonging to exchanges;
    /// since two nodes connected such nodes cannot necessarily indicate they are related. 
    /// </summary>
    [JsonPropertyName("maxOriginalInDegree")]
    public int MaxOriginalInDegree { init; get; } = 1000;

    [JsonPropertyName("maxOriginalOutDegree")]
    public int MaxOriginalOutDegree { init; get; } = 1000;

    [JsonPropertyName("maxNeighborsPerNode")]
    public int MaxNeighborsPerNode { init; get; } = 100;

    /// <summary>
    /// This ensures a block context for each transaction is 
    /// also included in the sampled communities. 
    /// This can provide more macro context for transactions. 
    /// </summary>
    [JsonPropertyName("includeBlockTxEdges")]
    public bool IncludeB2TEdges { init; get; } = false;

    /// <summary>
    /// For every Block node in the sampled community, also include this many
    /// earlier blocks (by height), chained through Block-[Follows]->Block edges.
    /// 0 (default) disables this.
    /// 
    /// Note that this will not count against the max node count per sampled community.
    /// </summary>
    [JsonPropertyName("earlierBlocksWindow")]
    public int EarlierBlocksWindow
    {
        init
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(EarlierBlocksWindow), "Value must be >= 0.");
            _earlierBlocksWindow = value;
        }
        get { return _earlierBlocksWindow; }
    }
    private int _earlierBlocksWindow = 0;

    /// <summary>
    /// For every Block node in the sampled community, also include this many
    /// later blocks (by height), chained through Block-[Follows]->Block edges.
    /// 0 (default) disables this.
    /// </summary>
    [JsonPropertyName("laterBlocksWindow")]
    public int LaterBlocksWindow
    {
        init
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(LaterBlocksWindow), "Value must be >= 0.");
            _laterBlocksWindow = value;
        }
        get { return _laterBlocksWindow; }
    }
    private int _laterBlocksWindow = 0;

    /// <summary>
    /// Lower bound (inclusive) of the block-height window that *every* height
    /// recorded on an edge must fall into for the edge to be included in the
    /// sampled neighborhood. Applies at every hop. 0 (default) means no lower bound.
    /// See MaxEdgeActivityHeight.
    ///
    /// Heights checked per edge type:
    ///   Credits (Tx->Script): CreationHeight and SpentHeight (SpentHeight is
    ///                         skipped when the output is still unspent);
    ///   Redeems (Script->Tx): CreationHeight and SpentHeight;
    ///   all others:           Height.
    /// An edge is excluded if any of its heights is outside the window.
    /// </summary>
    [JsonPropertyName("minEdgeActivityHeight")]
    public int MinEdgeActivityHeight
    {
        init
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(MinEdgeActivityHeight), "Value must be >= 0.");
            _minEdgeActivityHeight = value;
        }
        get { return _minEdgeActivityHeight; }
    }
    private int _minEdgeActivityHeight = 0;

    /// <summary>
    /// Upper bound (inclusive) of the block-height window that *every* height
    /// recorded on an edge must fall into for the edge to be included in the
    /// sampled neighborhood. Applies at every hop. int.MaxValue (default) means no upper bound.
    /// See MinEdgeActivityHeight for which heights are checked per edge type.
    /// </summary>
    [JsonPropertyName("maxEdgeActivityHeight")]
    public int MaxEdgeActivityHeight
    {
        init
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxEdgeActivityHeight), "Value must be >= 0.");
            _maxEdgeActivityHeight = value;
        }
        get { return _maxEdgeActivityHeight; }
    }
    private int _maxEdgeActivityHeight = int.MaxValue;

    /// <summary>
    /// If set, a Script node is included in the sampled neighborhood only if at least
    /// MinPercentage of its *incoming* edges (Credits) have CreationHeight inside the window.
    /// Tx, Block and Coinbase nodes are not judged: by design each is tied to a single block
    /// height. Can be combined with OutgoingActivityWindow and TotalActivityWindow; a Script
    /// must satisfy all configured ones. See ActivityScanLimit for how high-degree Scripts
    /// are judged.
    /// </summary>
    [JsonPropertyName("incomingActivityWindow")]
    public ActivityWindowOptions? IncomingActivityWindow { init; get; } = null;

    /// <summary>
    /// Same as IncomingActivityWindow, for the Script's *outgoing* edges (Redeems, judged on SpentHeight).
    /// </summary>
    [JsonPropertyName("outgoingActivityWindow")]
    public ActivityWindowOptions? OutgoingActivityWindow { init; get; } = null;

    /// <summary>
    /// Same as IncomingActivityWindow, for *all* of the Script's edges (incoming + outgoing).
    /// </summary>
    [JsonPropertyName("totalActivityWindow")]
    public ActivityWindowOptions? TotalActivityWindow { init; get; } = null;

    /// <summary>
    /// Maximum number of a Script's edges that are scanned to evaluate the activity
    /// windows (incomingActivityWindow, outgoingActivityWindow, totalActivityWindow).
    /// Bounds the cost of judging high-degree Scripts (e.g. exchange addresses with
    /// millions of edges), which would otherwise take minutes per node. Scripts with at
    /// most this many edges are judged exactly. For Scripts with more, the percentage is
    /// estimated over the scanned edges (roughly the most recently added ones, as Neo4j
    /// stores them), while minActivityCount is always checked against the exact degree.
    /// Default 50,000.
    /// </summary>
    [JsonPropertyName("activityScanLimit")]
    public int ActivityScanLimit { init; get; } = 50_000;

    /// <summary>
    /// As part of the random expansion, coinbase node gets
    /// the same selection probability as other nodes;
    /// hence, it may or may not be included in the sampled community.
    /// If this property is set to true, 
    /// if the coinbase node is included in the traversed neighborhood, 
    /// the algorithm will include it in the pseudo-randomly sampled subset 
    /// of the traversed neighborhood.
    /// </summary>
    [JsonPropertyName("forceIncludeCoinbaseNode")]
    public bool ForceIncludeCoinbaseNode {  init; get; } = true;

    /// <summary>
    /// When true (default), the walk does not expand a node whose expansion cannot place a
    /// Script node on the final layer. Every neighbor of a Script is a Tx, so a Script that
    /// would be expanded at the last allowed hop (hop == maxHops) is added to the sampled graph
    /// but its neighbors are not fetched; Tx nodes are always expanded since a Tx reaches a
    /// Script in one hop. Block and Coinbase nodes are never expanded regardless.
    ///
    /// Note: this prunes expansions that can only produce non-Script leaves; it does not make
    /// every leaf a Script. A Tx expanded at the last hop still adds its Tx, Block and Coinbase
    /// neighbors, and nodes skipped for high degree or by maxNodeCount/maxEdgeCount cut-offs
    /// also remain as non-Script leaves.
    ///
    /// Requires maxHops >= 1 (with maxHops 0 the root's Tx neighbors are the only layer).
    /// With the default maxHops 2 this removes the Tx fringe around hop-2 Scripts, so sampled
    /// graphs are smaller; raise maxHops or lower --min-node-count if too many roots are
    /// rejected for size.
    /// </summary>
    [JsonPropertyName("ensureScriptTerminalNodes")]
    public bool EnsureScriptTerminalNodes { init; get; } = true;
}

/// <summary>
/// A block-height window together with the minimum share of a Script node's activity
/// (incident edges) that must fall inside it for the Script to be included in a sampled
/// neighborhood. See BitcoinPanoramaSamplingAlgorithmOptions.IncomingActivityWindow,
/// OutgoingActivityWindow and TotalActivityWindow.
/// </summary>
public class ActivityWindowOptions
{
    /// <summary>Lowest block height (inclusive) of the window. Default 0.</summary>
    [JsonPropertyName("minHeight")]
    public int MinHeight { init; get; } = 0;

    /// <summary>Highest block height (inclusive) of the window. Default int.MaxValue.</summary>
    [JsonPropertyName("maxHeight")]
    public int MaxHeight { init; get; } = int.MaxValue;

    /// <summary>
    /// Minimum percentage (0..100) of the Script's activity whose activity height must
    /// lie inside [MinHeight, MaxHeight]. A Script with no activity in the considered
    /// direction does not satisfy the window. Default 0.
    /// </summary>
    [JsonPropertyName("minPercentage")]
    public double MinPercentage { init; get; } = 0;

    /// <summary>
    /// Minimum number of activities (edges) the Script must have in the considered
    /// direction (incoming, outgoing, or both for the total window), counted over its whole
    /// history, not only inside the window. Guards MinPercentage against tiny samples:
    /// e.g. with MinPercentage 50, a Script with 2 edges, 1 of them in the window, passes
    /// unless MinActivityCount is greater than 2. Default 1, i.e. at least one activity,
    /// which matches the rule that a Script with no activity does not satisfy the window.
    /// </summary>
    [JsonPropertyName("minActivityCount")]
    public int MinActivityCount { init; get; } = 1;
}
