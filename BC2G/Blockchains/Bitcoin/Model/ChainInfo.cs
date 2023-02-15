namespace BC2G.Blockchains.Bitcoin.Model;

public class ChainInfo
{
    [JsonPropertyName("chain")]
    public string Chain { get; set; } = string.Empty;

    [JsonPropertyName("blocks")]
    public int Blocks { get; set; }
}
