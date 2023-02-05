namespace BC2G.Graph.Model;

public class ScriptNode : Node, IComparable<ScriptNode>, IEquatable<ScriptNode>
{
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

    // VERY IMPORTANT TODO: REMOVE THIS CONSTRUCTOR, SHOULD NOT ALLOW CREATING NODE WITHOUT SPECIFYING NODE ID.
    public ScriptNode(string address, ScriptType scriptType) : this("0")
    {
        Address = address;
        ScriptType = scriptType;
    }

    // TODO: This constructor should be removed, we should NOT set specifically.
    public ScriptNode(string id, string address, ScriptType scriptType) : this(id)
    {
        Address = address;
        ScriptType = scriptType;
    }

    public ScriptNode(Neo4j.Driver.INode node) :
        this(node.ElementId,
            (string)node.Properties[
                Props.ScriptAddress.Name],

            Enum.Parse<ScriptType>((string)node.Properties[
                Props.ScriptType.Name]))
    { }

    public static ScriptNode GetCoinbaseNode()
    {
        return new ScriptNode("0");
    }

    public new double[] GetFeatures()
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
