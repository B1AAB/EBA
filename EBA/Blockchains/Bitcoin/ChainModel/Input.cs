namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Input : IEquatable<Input>
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

    public bool Equals(Input? other)
    {
        if (other is null) return false;
        return
            TxId == other.TxId &&
            OutputIndex == other.OutputIndex;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Input);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TxId, OutputIndex);
    }
}
