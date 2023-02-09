namespace BC2G.Graph.Model;

public class TxNode : Node, IComparable<TxNode>, IEquatable<TxNode>
{
    public Transaction? Tx { get; }

    public TxNode(string txid) : base(txid)
    { }

    public TxNode(Transaction tx) : base(tx.Txid)
    {
        Tx = tx;
    }

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
