namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Input : IEquatable<Input>
{
    [JsonPropertyName("coinbase")]
    public string Coinbase { set; get; } = string.Empty;

    [JsonPropertyName("txid")]
    public string TxId { set; get; } = string.Empty;

    [JsonPropertyName("vout")]
    public int Vout { set; get; }

    [JsonPropertyName("scriptSig")]
    public ScriptSig? ScriptSig { set; get; }

    [JsonPropertyName("txinwitness")]
    public List<string>? TxInputWitness { set; get; }

    [JsonPropertyName("sequence")]
    public long Sequence { set; get; }

    [JsonPropertyName("prevout")]
    public PrevOut? NullablePrevOut { set; get; }

    public PrevOut PrevOut
    {
        get
        {
            return
                NullablePrevOut ??
                throw new InvalidOperationException(
                    "PrevOut is null. " +
                    "Are you processing transactions in mempool?");
        }
    }

    public bool Equals(Input? other)
    {
        if (other is null) return false;
        return
            TxId == other.TxId &&
            Vout == other.Vout;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Input);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TxId, Vout);
    }
}
