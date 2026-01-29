namespace EBA.Blockchains.Bitcoin.ChainModel;

public class ChainInfo
{
    [JsonPropertyName("chain")]
    public string Chain { get; set; } = string.Empty;

    [JsonPropertyName("blocks")]
    public int Blocks { get; set; }
}
