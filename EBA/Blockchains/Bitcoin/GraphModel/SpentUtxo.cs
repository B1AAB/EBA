namespace EBA.Blockchains.Bitcoin.GraphModel;

public record SpentUTxO
{
    public string Txid { get; init; }
    public int Vout { get; init; }
    public bool Generated { get; init; }
    public long Value { get; init; }
    public long Height { get; init; }

    public SpentUTxO(string txid, int vout, bool generated, long value, long height)
    {
        Txid = txid;
        Vout = vout;
        Generated = generated;
        Value = value;
        Height = height;
    }

    public SpentUTxO(Input input)
    {
        Txid = input.TxId;
        Vout = input.Vout;
        Generated = input.PrevOut.Generated;
        Value = input.PrevOut.Value;
        Height = input.PrevOut.Height;
    }
}
