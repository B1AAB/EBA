namespace BC2G.Blockchains.Bitcoin.Graph;

public class ScriptNode : Node, IComparable<ScriptNode>, IEquatable<ScriptNode>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinScriptNode; }
    }

    public string Address { get; } = BitcoinAgent.Coinbase;
    public ScriptType ScriptType { get; } = ScriptType.Coinbase;

    public static new string Header
    {
        get
        {
            return string.Join(Delimiter, new string[]
            {
                Node.Header,
                "ScriptType"
            });
        }
    }

    public ScriptNode(string id) : base(id) { }

    public ScriptNode(Utxo utxo) : base(utxo.Id)
    {
        Address = utxo.Address;
        ScriptType = utxo.ScriptType;
    }

    public ScriptNode(string id, string address, ScriptType scriptType) : this(id)
    {
        Address = address;
        ScriptType = scriptType;
    }

    public ScriptNode(Neo4j.Driver.INode node) :
        this(node.ElementId,
            (string)node.Properties[Props.ScriptAddress.Name],
            Enum.Parse<ScriptType>((string)node.Properties[Props.ScriptType.Name]))
    { }

    public static ScriptNode GetCoinbaseNode()
    {
        return new ScriptNode(BitcoinAgent.Coinbase);
    }

    public static new string[] GetFeaturesName()
    {
        return
            new string[] { nameof(ScriptType) }
            .Concat(Node.GetFeaturesName()).ToArray();
    }

    public override double[] GetFeatures()
    {
        return
            new double[] { (double)ScriptType }
            .Concat(base.GetFeatures()).ToArray();
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
