namespace BC2G.Blockchains.Bitcoin;

public class T2TEdge : Edge<TxNode, TxNode>
{
    public T2TEdge(
        TxNode source, TxNode target,
        double value, EdgeType type, uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    { }

    public static T2TEdge Update(T2TEdge oldEdge, T2TEdge newEdge)
    {
        var sourceTx = newEdge.Source.Tx ?? oldEdge.Source.Tx;
        var sourceTxid = newEdge.Source.Id ?? oldEdge.Source.Id;
        var source = sourceTx is not null ? new TxNode(sourceTx) : new TxNode(sourceTxid);

        var targetTx = newEdge.Target.Tx ?? oldEdge.Target.Tx;
        var targetTxid = newEdge.Target.Id ?? oldEdge.Target.Id;
        var target = targetTx is not null ? new TxNode(targetTx) : new TxNode(targetTxid);

        return new T2TEdge(
            source, target,
            newEdge.Value,
            newEdge.Type,
            newEdge.Timestamp,
            newEdge.BlockHeight);
    }

    public T2TEdge Update(double value)
    {
        return new T2TEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
