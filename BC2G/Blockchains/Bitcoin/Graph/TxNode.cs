namespace BC2G.Blockchains.Bitcoin.Graph;

public class TxNode : Node, IComparable<TxNode>, IEquatable<TxNode>
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

    public TxNode(string txid) : base(txid)
    {
        Txid = txid;
    }

    public TxNode(
        string id, string txid, ulong? version,
        int? size, int? vSize, int? weight,
        long? lockTime) : base(id)
    {
        Txid = txid;
        Version = version;
        Size = size;
        VSize = vSize;
        Weight = weight;
        LockTime = lockTime;
    }

    public TxNode(
        string txid, ulong? version,
        int? size, int? vSize, int? weight,
        long? lockTime) :
        base(txid)
    {
        Txid = txid;
        Version = version;
        Size = size;
        VSize = vSize;
        Weight = weight;
        LockTime = lockTime;
    }

    // TODO: all the following double-casting is because of the type
    // normalization happens when bulk-loading data into neo4j.
    // Find a better solution.

    public TxNode(Neo4j.Driver.INode node) :
        this(
            txid: node.ElementId,
            version: ulong.Parse((string)node.Properties[Props.TxVersion.Name]),
            size: (int)(long)node.Properties[Props.TxSize.Name],
            vSize: (int)(long)node.Properties[Props.TxVSize.Name],
            weight: (int)(long)node.Properties[Props.TxWeight.Name],
            lockTime: (long)node.Properties[Props.TxLockTime.Name])
    { }

    public TxNode(Transaction tx) :
        this(tx.Txid, tx.Version, tx.Size, tx.VSize, tx.Weight, tx.LockTime)
    { }

    public static TxNode GetCoinbaseNode()
    {
        return new TxNode(BitcoinAgent.Coinbase);
    }

    public static new string[] GetFeaturesName()
    {
        return
            new string[] { nameof(Size), nameof(Weight), nameof(LockTime) }
            .Concat(Node.GetFeaturesName()).ToArray();
    }

    public override double[] GetFeatures()
    {
        // TODO: fix null values and avoid casting
        return
            new double[] { (double)Size, (double)Weight, (double)LockTime }
            .Concat(base.GetFeatures()).ToArray();
    }

    public int CompareTo(TxNode? other)
    {
        throw new NotImplementedException();
    }

    public bool Equals(TxNode? other)
    {
        throw new NotImplementedException();
    }
}
