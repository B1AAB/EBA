namespace EBA.Blockchains.Bitcoin.GraphModel;

public class ScriptNode : Node, IComparable<ScriptNode>, IEquatable<ScriptNode>
{
    public new static NodeKind Kind => NodeKind.Script;
    public override NodeKind NodeKind => Kind;    

    public string Address { get; }

    public ScriptType ScriptType { get; }

    public string HexBase64 { get; } = string.Empty;

    public string SHA256Hash { get; }

    public ScriptNode(
        string address,
        ScriptType scriptType,
        string sha256Hash,
        string hexBase64 = "",
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        base(
            id: sha256Hash,
            originalInDegree: originalIndegree,
            originalOutDegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb)
    {
        Address = address;
        ScriptType = scriptType;
        SHA256Hash = sha256Hash;
        HexBase64 = hexBase64;
    }

    public ScriptNode(
        ScriptPubKey scriptPubKey,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        base(id: scriptPubKey.SHA256Hash,
             originalInDegree: originalIndegree,
             originalOutDegree: originalOutdegree,
             outHopsFromRoot: hopsFromRoot,
             idInGraphDb: idInGraphDb)
    {
        Address = scriptPubKey.Address;
        ScriptType = scriptPubKey.ScriptType;
        SHA256Hash = scriptPubKey.SHA256Hash;

        if (ScriptType == ScriptType.nonstandard || ScriptType == ScriptType.NullData)
        {
            HexBase64 = scriptPubKey.Base64String;
        }
    }

    public override string GetIdPropertyName()
    {
        return nameof(SHA256Hash); // todo: this is not correct
    }

    public static new string[] GetFeaturesName()
    {
        return [nameof(SHA256Hash), nameof(ScriptType), .. Node.GetFeaturesName()];
    }

    public override string[] GetFeatures()
    {
        return [SHA256Hash, ((double)ScriptType).ToString(), .. base.GetFeatures()];
    }

    public override bool HasNullFeatures()
    {
        return base.HasNullFeatures();
    }

    public override int GetHashCode()
    {
        // Do not add ID here, because ID is generated
        // in a multi-threaded process, hence cannot
        // guarantee a node's ID is reproducible.
        return HashCode.Combine(Address, ScriptType);
    }

    public int CompareTo(ScriptNode? other)
    {
        if (other == null) return -1;
        var r = Address.CompareTo(other.Address);
        if (r != 0) return r;
        return ScriptType.CompareTo(other.ScriptType);
    }

    public bool Equals(ScriptNode? other)
    {
        if (other == null)
            return false;

        return
            Address == other.Address &&
            ScriptType == other.ScriptType;
    }

    public override string ToString()
    {
        return string.Join(
            Delimiter,
            new string[]
            {
                base.ToString(),
                ScriptType.ToString("d")
            });
    }
}
