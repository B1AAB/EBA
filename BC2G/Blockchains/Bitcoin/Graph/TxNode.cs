namespace BC2G.Blockchains.Bitcoin.Graph;

public class TxNode : TxNode<ContextBase>,
    IComparable<TxNode<ContextBase>>,
    IEquatable<TxNode<ContextBase>>
{
    public TxNode(string txid) : base(txid, new ContextBase()) { }

    public TxNode(
        string id,
        string txid,
        ulong? version,
        int? size,
        int? vSize,
        int? weight,
        long? lockTime) : base(
            id: id,
            txid: txid,
            version: version,
            size: size,
            vSize: vSize,
            weight: weight,
            lockTime: lockTime,
            context: new ContextBase())
    { }

    public TxNode(Transaction tx) : base(tx, new ContextBase()) { }

    public static TxNode GetCoinbaseNode()
    {
        return new TxNode(BitcoinAgent.Coinbase);
    }
}

public class TxNode<T> : Node<T>, IComparable<TxNode<T>>, IEquatable<TxNode<T>>
    where T: IContext
{
    public new static GraphComponentType ComponentType { get { return GraphComponentType.BitcoinTxNode; } }
    public override GraphComponentType GetGraphComponentType() { return ComponentType; }

    public string Txid { get; }
    public ulong? Version { get; }
    public int? Size { get; }
    public int? VSize { get; }
    public int? Weight { get; }
    public long? LockTime { get; }

    public Transaction? Tx { get; }

    public TxNode(string txid, T context) : base(txid, context)
    {
        Txid = txid;
    }

    public TxNode(string id,
        string txid,
        ulong? version,
        int? size,
        int? vSize,
        int? weight,
        long? lockTime,
        T context) : base(id, context)
    {
        Txid = txid;
        Version = version;
        Size = size;
        VSize = vSize;
        Weight = weight;
        LockTime = lockTime;
    }

    public TxNode(
        string txid,
        ulong? version,
        int? size,
        int? vSize,
        int? weight,
        long? lockTime,
        T context) :
        base(txid, context)
    {
        Txid = txid;
        Version = version;
        Size = size;
        VSize = vSize;
        Weight = weight;
        LockTime = lockTime;
    }

    public TxNode(Transaction tx, T context) :
        this(tx.Txid, tx.Version, tx.Size, tx.VSize, tx.Weight, tx.LockTime, context)
    { }

    public override string GetUniqueLabel()
    {
        return Txid;
    }

    public static TxNode<T> GetCoinbaseNode()
    {
        return new TxNode<T>(BitcoinAgent.Coinbase, new T());
    }

    public static TxNode<T> CreateTxNode(Neo4j.Driver.INode node, T context)
    {
        // TODO: all the following double-casting is because of the type
        // normalization happens when bulk-loading data into neo4j.
        // Find a better solution.

        node.Properties.TryGetValue(Props.TxVersion.Name, out var v);
        ulong? version = v == null ? null : ulong.Parse((string)v);

        node.Properties.TryGetValue(Props.TxSize.Name, out var s);
        int? size = s == null ? null : (int)(long)s;

        node.Properties.TryGetValue(Props.TxVSize.Name, out var vs);
        int? vSize = vs == null ? null : (int)(long)vs;

        node.Properties.TryGetValue(Props.TxWeight.Name, out var w);
        int? weight = w == null ? null : (int)(long)w;

        node.Properties.TryGetValue(Props.TxLockTime.Name, out var t);
        long? lockTime = t == null ? null : (long)t;

        return new TxNode<T>(
            txid: node.ElementId,
            version: version,
            size: size,
            vSize: vSize,
            weight: weight,
            lockTime: lockTime,
            context: context);
    }

    public static new string[] GetFeaturesName()
    {
        return 
        [
            nameof(Size),
            nameof(Weight),
            nameof(LockTime),
            .. 
            Node<T>.GetFeaturesName()
        ];
    }

    public override string[] GetFeatures()
    {
        // TODO: fix null values and avoid casting
        return
        [
            (Size == null ? double.NaN : (double)Size).ToString(),
            (Weight == null ? double.NaN :(double)Weight).ToString(),
            (LockTime == null ? double.NaN :(double) LockTime).ToString(),
            .. base.GetFeatures(),
        ];
    }

    public int CompareTo(TxNode<T>? other)
    {
        throw new NotImplementedException();
    }

    public bool Equals(TxNode<T>? other)
    {
        throw new NotImplementedException();
    }
}
