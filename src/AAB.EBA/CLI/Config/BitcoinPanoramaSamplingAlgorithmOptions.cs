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
}
