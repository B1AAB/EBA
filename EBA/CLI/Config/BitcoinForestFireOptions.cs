namespace EBA.CLI.Config;

public class BitcoinForestFireOptions
{
    [JsonPropertyName("maxHops")]
    public int MaxHops { init; get; } = 2;

    [JsonPropertyName("queryLimit")]
    public int QueryLimit { init; get; } = 1000;

    [JsonPropertyName("reductionFactor")]
    public double NodeCountReductionFactorByHop { init; get; } = 4.0;

    [JsonPropertyName("nodeCountAtRoot")]
    public int NodeSamplingCountAtRoot { init; get; } = 100;
}
