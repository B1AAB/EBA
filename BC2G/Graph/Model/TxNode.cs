namespace BC2G.Graph.Model;

public class TxNode : Node, IComparable<TxNode>, IEquatable<TxNode>
{
    public Transaction Tx { get; }    

    public TxNode(Transaction tx) : base(tx.Txid)
    {
        Tx = tx;
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
