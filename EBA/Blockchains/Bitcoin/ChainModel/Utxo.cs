namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Utxo
{
    public string Id { set; get; } = string.Empty;

    public string Txid { get { return GetTxid(Id); } }

    public string Address { set; get; } = string.Empty;

    public long Value { set; get; }

    public ScriptType ScriptType { set; get; }

    public bool IsGenerated { set; get; }

    public long CreatedInBlockHeight { get; }

    public long? SpentInBlockHeight { set; get; }

    public Utxo(
        string id, string? address, long value, ScriptType scriptType, bool isGenerated,
        long createdInBlockHeight,
        long? spentInBlockHeight = null)
    {
        Id = id;
        Address = address ?? Id;
        Value = value;
        ScriptType = scriptType;
        IsGenerated = isGenerated;
        CreatedInBlockHeight = createdInBlockHeight;
        SpentInBlockHeight = spentInBlockHeight;
    }

    public Utxo(
        ScriptPubKey scriptPubKey,
        string? address,
        long value,
        ScriptType scriptType,
        bool isGenerated,
        long createdInHeight,
        long? spentInHeight = null) :
    this(
        scriptPubKey.SHA256HashString,
        address,
        value,
        scriptType,
        isGenerated,
        createdInHeight,
        spentInHeight)
    { }

    public static string GetId(string txid, int voutN)
    {
        return $"{voutN}-{txid}";
    }
    public static string GetTxid(string id)
    {
        return id.Split('-')[1];
    }

    public static string GetHeader()
    {
        return string.Join(
            '\t',
            "Id",
            "Value",
            "CreatedInBlockHeights",
            "CreatedInBlockHeightsCount",
            "SpentInBlockHeights",
            "SpentInBlockHeightsCount",
            "ScriptType",
            "IsGenerated(0=No,1=Yes)");
    }
}
