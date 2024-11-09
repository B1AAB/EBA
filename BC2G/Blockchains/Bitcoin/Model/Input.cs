namespace BC2G.Blockchains.Bitcoin.Model;

public class Input
{
    [JsonPropertyName("coinbase")]
    public string Coinbase { set; get; } = string.Empty;

    [JsonPropertyName("txid")]
    public string TxId { set; get; } = string.Empty;

    [JsonPropertyName("vout")]
    public int OutputIndex { set; get; }

    [JsonPropertyName("scriptSig")]
    public ScriptSig? ScriptSig { set; get; }

    [JsonPropertyName("txinwitness")]
    public List<string>? TxInputWitness { set; get; }

    [JsonPropertyName("sequence")]
    public long Sequence { set; get; }

    [JsonPropertyName("prevout")]
    public PrevOut? PrevOut { set; get; }
}
