namespace BC2G.Graph;

public class Node : IComparable<Node>, IEquatable<Node>
{
    public string Id { get; } = "0";
    public string Address { get; } = BitcoinAgent.Coinbase;
    public ScriptType ScriptType { get; } = ScriptType.Coinbase;

    public int InDegree { get { return IncomingEdges.Count; } }
    public int OutDegree { get { return OutgoingEdges.Count; } }

    public List<Edge> IncomingEdges { private set; get; } = new();
    public List<Edge> OutgoingEdges { private set; get; } = new();

    public static string Header
    {
        get
        {
            return string.Join(_delimiter, new string[]
            {
                "Id",
                "ScriptType"
            });
        }
    }

    private const string _delimiter = "\t";

    /// <summary>
    /// This constructor creates the Coinbase node.
    /// </summary>
    public Node() { }

    public Node(string address, ScriptType scriptType)
    {
        Address = address;
        ScriptType = scriptType;
    }

    // TODO: This constructor should be removed, we should NOT set specifically.
    public Node(string id, string address, ScriptType scriptType)
    {
        Id = id;
        Address = address;
        ScriptType = scriptType;
    }

    public Node(INode node) :
        this(node.ElementId,
            (string)node.Properties["Address"],
            Enum.Parse<ScriptType>((string)node.Properties["ScriptType"]))
    { }

    public void AddIncomingEdges(Edge incomingEdge)
    {
        IncomingEdges.Add(incomingEdge);
    }

    public void AddIncomingEdges(List<Edge> incomingEdges)
    {
        IncomingEdges.AddRange(incomingEdges);
    }

    public void AddOutgoingEdges(List<Edge> outgoingEdges)
    {
        OutgoingEdges.AddRange(outgoingEdges);
    }

    public void AddOutgoingEdges(Edge outgoingEdge)
    {
        OutgoingEdges.Add(outgoingEdge);
    }

    public double[] GetFeatures()
    {
        return new double[] { (double)ScriptType };
    }

    public override string ToString()
    {
        return string.Join(
            _delimiter, 
            new string[] { Id, ScriptType.ToString("d") });
    }

    public override int GetHashCode()
    {
        // Do not add ID here, because ID is generated at
        // runtime in a multi-threaded process, hence cannot
        // guarantee a node's ID is reproducible..
        return HashCode.Combine(Address, ScriptType);
    }

    public int CompareTo(Node? other)
    {
        if (other == null) return -1;
        var r = Address.CompareTo(other.Address);
        if (r != 0) return r;
        return ScriptType.CompareTo(other.ScriptType);
    }

    
    public bool Equals(Node? other)
    {
        if (other == null) 
            return false;

        return 
            Address == other.Address && 
            ScriptType == other.ScriptType;
    }
}
