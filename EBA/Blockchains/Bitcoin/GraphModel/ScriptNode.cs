using EBA.Graph.Bitcoin;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class ScriptNode : Node, IComparable<ScriptNode>, IEquatable<ScriptNode>
{
    public static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinScriptNode; }
    }
    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinScriptNode;
    }

    // TODO: since there is a CoinbaseNode type, this default should change
    public string Address { get; } = BitcoinChainAgent.Coinbase.ToString();
    // TODO: since there is a CoinbaseNode type, this default should change
    public ScriptType ScriptType { get; } = ScriptType.Coinbase;

    public ScriptNode(
        string address,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        base(
            id: address,
            originalInDegree: originalIndegree,
            originalOutDegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb)
    { }

    public ScriptNode(Utxo utxo) : base(utxo.Id)
    {
        Address = utxo.Address;
        ScriptType = utxo.ScriptType;
    }

    public ScriptNode(
        string address,
        ScriptType scriptType,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        this(
            address: address,
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            hopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb)
    {
        Address = address;
        ScriptType = scriptType;
    }

    public override string GetIdPropertyName()
    {
        return nameof(Address);
    }

    public static ScriptNode GetCoinbaseNode()
    {
        return new ScriptNode(NodeLabels.Coinbase.ToString());
    }

    public static new string[] GetFeaturesName()
    {
        return [nameof(Address), nameof(ScriptType), .. Node.GetFeaturesName()];
    }

    public override string[] GetFeatures()
    {
        return [Address, ((double)ScriptType).ToString(), .. base.GetFeatures()];
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
