namespace EBA.CLI.Config;

public class BitcoinOptions(long timestamp)
{
    public BitcoinTraverseOptions Traverse { init; get; } = new(timestamp);
    public BitcoinDedupOptions Dedup { init; get; } = new();
    public BitcoinGraphSampleOptions GraphSample { init; get; } = new();
}
