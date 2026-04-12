namespace EBA.CLI.Config;

public class BitcoinOptions(long timestamp)
{
    [JsonConstructor]
    public BitcoinOptions() : this(DateTimeOffset.Now.ToUnixTimeSeconds()) { }

    public BitcoinTraverseOptions Traverse { init; get; } = new BitcoinTraverseOptions(timestamp);
    public BitcoinDedupOptions Dedup { init; get; } = new();
    public BitcoinGraphSampleOptions GraphSample { init; get; } = new();
    public BitcoinMapMarketOptions MapMarket { init; get; } = new();
    public BitcoinAugmentorOptions Augmentor { init; get; } = new();
    public BitcoinMapSpendsOptions MapSpends { init; get; } = new();
}
