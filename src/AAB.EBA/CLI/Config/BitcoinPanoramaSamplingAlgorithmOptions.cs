namespace AAB.EBA.CLI.Config;

public class BitcoinPanoramaSamplingAlgorithmOptions
{
    [JsonPropertyName("maxHops")]
    public int MaxHops { init; get; } = 2;

    [JsonPropertyName("queryLimit")]
    public int QueryLimit { init; get; } = 1000;

    [JsonPropertyName("reductionFactor")]
    public double NodeCountReductionFactorByHop { init; get; } = 4.0;

    /// <summary>
    /// This could be used to exclude high degree nodes, 
    /// such as mixer nodes or nodes belonging to exchanges;
    /// since two nodes connected such nodes cannot necessarily indicate they are related. 
    /// </summary>
    [JsonPropertyName("nodeCountAtRoot")]
    public int NodeSamplingCountAtRoot { init; get; } = 100;

    /// <summary>
    /// This could be used to exclude high degree nodes, 
    /// such as mixer nodes or nodes belonging to exchanges;
    /// since two nodes connected such nodes cannot necessarily indicate they are related. 
    /// </summary>
    [JsonPropertyName("maxOriginalInDegree")]
    public int MaxOriginalInDegree { init; get; } = 1000;

    [JsonPropertyName("maxOriginalOutDegree")]
    public int MaxOriginalOutDegree { init; get; } = 1000;

    /// <summary>
    /// This ensures a block context for each transaction is 
    /// also included in the sampled communities. 
    /// This can provide more macro context for transactions. 
    /// </summary>
    [JsonPropertyName("includeBlockTxEdges")]
    public bool IncludeB2TEdges { init; get; } = false;
}
