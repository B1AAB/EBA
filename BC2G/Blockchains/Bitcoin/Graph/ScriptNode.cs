namespace BC2G.Blockchains.Bitcoin.Graph;

public class ScriptNode : ScriptNode<ContextBase>,
    IComparable<ScriptNode<ContextBase>>,
    IEquatable<ScriptNode<ContextBase>>
{
    public ScriptNode(string id) : base(id, new ContextBase()) 
    { }

    public ScriptNode(Utxo utxo) : base(utxo, new ContextBase())
    { }

    public static ScriptNode GetCoinbaseNode()
    {
        return new ScriptNode(BitcoinAgent.Coinbase);
    }
}

public class ScriptNode<T> : Node<T>, IComparable<ScriptNode<T>>, IEquatable<ScriptNode<T>>
    where T: IContext
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
    public string Address { get; } = BitcoinAgent.Coinbase;
    // TODO: since there is a CoinbaseNode type, this default should change
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

    public ScriptNode(string id, T context) : base(id, context)
    { }

    public ScriptNode(Utxo utxo, T context) : base(utxo.Id, context)
    {
        Address = utxo.Address;
        ScriptType = utxo.ScriptType;
    }

    public ScriptNode(string id, string address, ScriptType scriptType, T context) : this(id, context)
    {
        Address = address;
        ScriptType = scriptType;
    }

    public ScriptNode(Neo4j.Driver.INode node, T context) :
        this(node.ElementId,
            (string)node.Properties[Props.ScriptAddress.Name],
            Enum.Parse<ScriptType>((string)node.Properties[Props.ScriptType.Name]),
            context)
    { }

    public override string GetUniqueLabel()
    {
        return Address;
    }

    public static ScriptNode<T> GetCoinbaseNode(T context)
    {
        return new ScriptNode<T>(BitcoinAgent.Coinbase, context);
    }

    public static ScriptNode<T> GetCoinbaseNode()
    {
        return new ScriptNode<T>(BitcoinAgent.Coinbase, new T());
    }

    public static new string[] GetFeaturesName()
    {
        return [nameof(Address), nameof(ScriptType), .. Node<T>.GetFeaturesName()];
    }

    public override string[] GetFeatures()
    {
        return [Address, ((double)ScriptType).ToString(), .. base.GetFeatures()];
    }

    public override int GetHashCode()
    {
        // Do not add ID here, because ID is generated
        // in a multi-threaded process, hence cannot
        // guarantee a node's ID is reproducible.
        return HashCode.Combine(Address, ScriptType);
    }

    public int CompareTo(ScriptNode<T>? other)
    {
        if (other == null) return -1;
        var r = Address.CompareTo(other.Address);
        if (r != 0) return r;
        return ScriptType.CompareTo(other.ScriptType);
    }

    public bool Equals(ScriptNode<T>? other)
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
