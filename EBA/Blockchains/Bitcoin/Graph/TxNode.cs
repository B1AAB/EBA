using EBA.Graph.Bitcoin;

namespace EBA.Blockchains.Bitcoin.Graph;

// A note on the nullable properties: 
// These properties can be null when the Tx
// this type corresponds to is an input transaction (vin) 
// when reading transactions from the Bitcoin blockchain.
// In this case, minimal information about the Tx is available, 
// as opposed to when the Tx is an output transaction (vout).
// An example is:
//  {"txid": "0437cd7f8525ceed2324359c2d0ba26006d92d856a9c20fa0241106ee5a597c9"}
// This Tx is referenced as input in Tx #2 at block height 170. 

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
        string txid,
        ulong? version,
        int? size,
        int? vSize,
        int? weight,
        long? lockTime) : base(txid)
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
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        base(
            txid,
            originalInDegree: originalIndegree,
            originalOutDegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb)
    {
        Txid = txid;
        Version = version;
        Size = size;
        VSize = vSize;
        Weight = weight;
        LockTime = lockTime;
    }

    public TxNode(Transaction tx) :
        this(
            tx.Txid,
            tx.Version,
            tx.Size,
            tx.VSize,
            tx.Weight,
            tx.LockTime)
    { }

    public override string GetIdPropertyName()
    {
        return nameof(Txid);
    }

    public static TxNode CreateTxNode(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double hopsFromRoot)
    {
        // All the following double-casting is because of the type
        // normalization happens when bulk-loading data into neo4j.

        string? candidateTxid = 
            (node.Properties.GetValueOrDefault(Props.Txid.Name)?.ToString()) 
            ?? throw new ArgumentNullException(Props.Txid.Name);
        string txid = candidateTxid;

        string? v = node.Properties.GetValueOrDefault(Props.TxVersion.Name)?.ToString();
        ulong? version = v == null ? null : ulong.Parse(v);

        node.Properties.TryGetValue(Props.TxSize.Name, out var s);
        int? size = s == null ? null : (int)(long)s;

        node.Properties.TryGetValue(Props.TxVSize.Name, out var vs);
        int? vSize = vs == null ? null : (int)(long)vs;

        node.Properties.TryGetValue(Props.TxWeight.Name, out var w);
        int? weight = w == null ? null : (int)(long)w;

        node.Properties.TryGetValue(Props.TxLockTime.Name, out var t);
        long? lockTime = t == null ? null : (long)t;

        return new TxNode(
            txid: txid,
            version: version,
            size: size,
            vSize: vSize,
            weight: weight,
            lockTime: lockTime,
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            hopsFromRoot: hopsFromRoot,
            idInGraphDb: node.ElementId);
    }

    public static TxNode GetCoinbaseNode()
    {
        return new TxNode(NodeLabels.Coinbase.ToString());
    }

    public static new string[] GetFeaturesName()
    {
        return
        [
            nameof(Size),
            nameof(Weight),
            nameof(LockTime),
            .. Node.GetFeaturesName()
        ];
    }

    public override bool HasNullFeatures()
    {
        return Size == null
               || Version == null
               || VSize == null
               || Weight == null
               || LockTime == null
               || base.HasNullFeatures();
    }

    public override string[] GetFeatures()
    {
        return
        [
            (Size == null ? double.NaN : (double)Size).ToString(),
            (Weight == null ? double.NaN :(double)Weight).ToString(),
            (LockTime == null ? double.NaN :(double) LockTime).ToString(),
            .. base.GetFeatures(),
        ];
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
