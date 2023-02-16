namespace BC2G.Blockchains.Bitcoin.Graph;

public class TxNode : Node, IComparable<TxNode>, IEquatable<TxNode>
{
    public string? Txid { get; }
    public int? Version { get; }
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
        string id, string? txid, int? version,
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
        string txid, int? version,
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

    public TxNode(Transaction tx) :
        this(tx.Txid, tx.Version, tx.Size, tx.VSize, tx.Weight, tx.LockTime)
    { }

    public static TxNode GetCoinbaseNode()
    {
        return new TxNode(BitcoinAgent.Coinbase);
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
