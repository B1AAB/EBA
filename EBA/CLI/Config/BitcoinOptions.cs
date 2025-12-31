namespace EBA.CLI.Config;

public class BitcoinOptions
{
    public BitcoinTraverseOptions Traverse { init; get; } = new();
    public BitcoinDedupOptions Dedup { init; get; } = new();
    public BitcoinGraphSampleOptions GraphSample { init; get; } = new();
}
