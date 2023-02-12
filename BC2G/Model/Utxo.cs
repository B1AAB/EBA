namespace BC2G.Model;

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

    /// <summary>
    /// A list of comma-separated block heights where this txo 
    /// is defined as output.
    /// </summary>
    [Required]
    public string CreatedIn
    {
        set { _createdIn = value; }
        get { return _createdIn; }
    }
    private string _createdIn = string.Empty;

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
    public string ReferencedIn
    {
        set { _refdIn = value; }
        get { return _refdIn; }
    }
    private string _refdIn = string.Empty;

    public int ReferencedInCount
    {
        set { _refdInCount = value; }
        get { return _refdInCount; }
    }
    private int _refdInCount;

    // This constructor is required by EF.
    public Utxo(
        string id, string address, double value, ScriptType scriptType,
        string createdIn, string? referencedIn = null)
    {
        Id = id;
        Address = address;
        Value = value;
        ScriptType = scriptType;

        if (!string.IsNullOrEmpty(createdIn))
        {
            CreatedInCount = 1;
            CreatedIn = createdIn;
        }
        if (!string.IsNullOrEmpty(referencedIn))
        {
            ReferencedInCount = 1;
            ReferencedIn = referencedIn;
        }
    }

    public Utxo(
        string txid, int voutN, string address, double value, ScriptType scriptType,
        string createdIn, string? referencedIn = null) :
        this(GetId(txid, voutN), address, value, scriptType, createdIn, referencedIn)
    { }

    public static string GetId(string txid, int voutN)
    {
        return $"{voutN}-{txid}";
    }
    public static string GetTxid(string id)
    {
        return id.Split('-')[1];
    }

    public void AddReferencedIn(string address)
    {
        UpdateRefs(ref _refdIn, ref _refdInCount, address);
    }

    public void AddCreatedIn(string address)
    {
        UpdateRefs(ref _createdIn, ref _createdInCount, address);
    }

    private static void UpdateRefs(ref string refs, ref int counts, string newRef)
    {
        if (string.IsNullOrWhiteSpace(newRef))
            return;

        var existingRefs = refs.Split(_delimiter);
        if (existingRefs.Contains(newRef))
            return;

        counts++;
        if (existingRefs.Length > 0)
            refs += _delimiter;
        refs += newRef;
    }
}
