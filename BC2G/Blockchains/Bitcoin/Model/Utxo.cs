using System.Data;

namespace BC2G.Blockchains.Bitcoin.Model;

[Table("Utxo")]
public class Utxo
{
    private const char _delimiter = ';';

    [Required]
    public string Id { set; get; } = string.Empty;

    /// <summary>
    /// This is used for optimistic concurrency. See:
    /// https://www.npgsql.org/efcore/modeling/concurrency.html
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    public string Txid { get { return GetTxid(Id); } }

    [Required]
    public string Address { set; get; } = string.Empty;

    [Required]
    public double Value { set; get; }

    [Required]
    public ScriptType ScriptType { set; get; }


    public string CreatedInBlockHash
    {
        set { _createdInBlockHash = value; }
        get { return _createdInBlockHash; }
    }
    private string _createdInBlockHash = string.Empty;

    /// <summary>
    /// A list of comma-separated block heights where this txo 
    /// is defined as output.
    /// </summary>
    [Required]
    public string CreatedInBlockHeight
    {
        set { _createdInBlockHeight = value; }
        get { return _createdInBlockHeight; }
    }
    private string _createdInBlockHeight = string.Empty;

    public int CreatedInCount
    {
        set { _createdInCount = value; }
        get { return _createdInCount; }
    }
    private int _createdInCount;

    /// <summary>
    /// A list of comma-separated block heights where this txo
    /// is defined as input. If this list is empty, the tx is 
    /// unspent (utxo).
    /// </summary>
    public string ReferencedInBlockHeight
    {
        set { _refdInHeight = value; }
        get { return _refdInHeight; }
    }
    private string _refdInHeight = string.Empty;

    public int ReferencedInCount
    {
        set { _refdInCount = value; }
        get { return _refdInCount; }
    }
    private int _refdInCount;

    public Utxo(
        string id, string? address, double value, ScriptType scriptType,
        string? createdInBlockHash = null, string? createdInBlockHeight = null,
        string? referencedInBlockHeight = null)
    {
        Id = id;
        Address = address ?? Id;
        Value = value;
        ScriptType = scriptType;

        if (string.IsNullOrEmpty(createdInBlockHash) && string.IsNullOrEmpty(createdInBlockHeight))
            throw new NoNullAllowedException("Created-in block hash and height cannot be both null.");

        if (!string.IsNullOrEmpty(createdInBlockHeight))
        {
            CreatedInCount = 1;
            CreatedInBlockHeight = createdInBlockHeight;
        }

        if (!string.IsNullOrEmpty(createdInBlockHash))
            CreatedInBlockHash = createdInBlockHash;

        if (!string.IsNullOrEmpty(referencedInBlockHeight))
        {
            ReferencedInCount = 1;
            ReferencedInBlockHeight = referencedInBlockHeight;
        }
    }

    public Utxo(
        string txid, int voutN, string? address, double value, ScriptType scriptType,
        string? createdInBlockHash = null, string? createdInHeight = null, string? referencedInHeight = null) :
        this(GetId(txid, voutN), address, value, scriptType, createdInBlockHash, createdInHeight, referencedInHeight)
    { }

    // This constructor is required by EF.
    public Utxo(
        string id, string? address, double value, ScriptType scriptType,
        string createdInBlockHeight,
        string? referencedInBlockHeight = null)
        : this(id, address, value, scriptType,
              createdInBlockHash: null, createdInBlockHeight: createdInBlockHeight,
              referencedInBlockHeight: referencedInBlockHeight)
    { }

    public static string GetId(string txid, int voutN)
    {
        return $"{voutN}-{txid}";
    }
    public static string GetTxid(string id)
    {
        return id.Split('-')[1];
    }

    public void AddCreatedIn(string? height, string? blockHash = null)
    {
        if (string.IsNullOrEmpty(height) && string.IsNullOrEmpty(blockHash))
            throw new NoNullAllowedException("Created-in block hash and height cannot be both null.");

        if (!string.IsNullOrEmpty(height))
            UpdateRefs(ref _createdInBlockHeight, ref _createdInCount, height);

        if (!string.IsNullOrEmpty(blockHash))
        {
            var dummy = 0;
            UpdateRefs(ref _createdInBlockHash, ref dummy, blockHash);
        }
    }

    public void AddReferencedIn(string height)
    {
        UpdateRefs(ref _refdInHeight, ref _refdInCount, height);
    }

    private static void UpdateRefs(ref string refs, ref int counts, string newRef)
    {
        if (string.IsNullOrWhiteSpace(newRef))
            return;

        var existingRefs = refs.Split(_delimiter, StringSplitOptions.RemoveEmptyEntries);
        if (existingRefs.Contains(newRef))
            return;

        counts++;
        if (existingRefs.Length > 0)
            refs += _delimiter;
        refs += newRef;
    }
}
