using System.Data;

namespace BC2G.Blockchains.Bitcoin.Model;

public class Utxo
{
    public string Id { set; get; } = string.Empty;

    public string Txid { get { return GetTxid(Id); } }

    public string Address { set; get; } = string.Empty;

    public long Value { set; get; }

    public ScriptType ScriptType { set; get; }

    public bool IsGenerated { set; get; }

    public ReadOnlyCollection<long> CreatedInBlockHeight
    {
        get { return _createdInBlockHeight.AsReadOnly(); }
    }
    private readonly List<long> _createdInBlockHeight = [];

    public int CreatedInCount
    {
        get { return _createdInBlockHeight.Count; }
    }

    public ReadOnlyCollection<long> SpentInBlockHeight
    {
        get { return _spentInHeight.AsReadOnly(); }
    }
    private readonly List<long> _spentInHeight = [];

    public int SpentInCount
    {
        get { return _spentInHeight.Count; }
    }

    public Utxo(
        string id, string? address, long value, ScriptType scriptType, bool isGenerated,
        List<long>? createdInBlockHeights = null,
        List<long>? spentInBlockHeights = null)
    {
        Id = id;
        Address = address ?? Id;
        Value = value;
        ScriptType = scriptType;
        IsGenerated = isGenerated;

        if ((createdInBlockHeights == null || createdInBlockHeights.Count == 0) &&
            (spentInBlockHeights == null || spentInBlockHeights.Count == 0))
            throw new NoNullAllowedException("Created-in and spent-in list of blocks cannot be both null/empty.");

        if (createdInBlockHeights != null)
            _createdInBlockHeight = createdInBlockHeights;

        if (spentInBlockHeights != null)
            _spentInHeight = spentInBlockHeights;
    }

    public Utxo(
        string txid, int voutN, string? address, long value, ScriptType scriptType, bool isGenerated,
        List<long>? createdInHeights = null, List<long>? spentInHeights = null) :
        this(GetId(txid, voutN), address, value, scriptType, isGenerated, createdInHeights, spentInHeights)
    { }

    public Utxo(
        string id, string? address, long value, ScriptType scriptType, bool isGenerated,
        long? createdInHeight = null, long? spentInHeight = null)
    {
        Id = id;
        Address = address ?? Id;
        Value = value;
        ScriptType = scriptType;
        IsGenerated = isGenerated;

        if (createdInHeight != null)
            _createdInBlockHeight.Add((long)createdInHeight);

        if (spentInHeight != null)
            _spentInHeight.Add((long)spentInHeight);
    }

    public Utxo(
        string txid, int voutN, string? address, long value, ScriptType scriptType, bool isGenerated,
        long? createdInHeight = null, long? spentInHeight = null) :
        this(GetId(txid, voutN), address, value, scriptType, isGenerated, createdInHeight, spentInHeight)
    { }

    public static string GetId(string txid, int voutN)
    {
        return $"{voutN}-{txid}";
    }
    public static string GetTxid(string id)
    {
        return id.Split('-')[1];
    }

    public void AddCreatedIn(long height)
    {
        if (!_createdInBlockHeight.Contains(height))
            _createdInBlockHeight.Add(height);
    }

    public void AddSpentIn(long height)
    {
        if (!_spentInHeight.Contains(height))
            _spentInHeight.Add(height);
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

    public override string ToString()
    {
        return string.Join(
            '\t',
            Id,
            Value.ToString(),
            string.Join(';', CreatedInBlockHeight),
            CreatedInCount,
            string.Join(";", SpentInBlockHeight),
            SpentInCount,
            ScriptType,
            IsGenerated ? "1" : "0");
    }
}
