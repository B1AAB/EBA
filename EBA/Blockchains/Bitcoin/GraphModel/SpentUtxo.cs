namespace EBA.Blockchains.Bitcoin.GraphModel;

public record SpentUtxo
{
    public string Txid { get; init; }
    public int Vout { get; init; }
    public bool Generated { get; init; }
    public long Value { get; init; }
    public long Height { get; init; }

    public SpentUtxo(string txid, int vout, bool generated, long value, long height)
    {
        Txid = txid;
        Vout = vout;
        Generated = generated;
        Value = value;
        Height = height;
    }

    public SpentUtxo(Input input)
    {
        Txid = input.TxId;
        Vout = input.Vout;
        Generated = input.PrevOut.Generated;
        Value = input.PrevOut.Value;
        Height = input.PrevOut.Height;
    }
}
