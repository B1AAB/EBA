namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2TEdge : Edge<TxNode, TxNode>
{
    public T2TEdge(
        TxNode source, TxNode target,
        long value, EdgeType type, uint timestamp, long blockHeight) :
        base(source, target, value, type, type == EdgeType.Transfers ? EdgeLabel.T2TTransfer : EdgeLabel.T2TFee, timestamp, blockHeight)
    { }

    public static T2TEdge Update(T2TEdge oldEdge, T2TEdge newEdge)
    {
        var source = new TxNode(
            newEdge.Source.Txid,
            newEdge.Source.Version ?? oldEdge.Source.Version,
            newEdge.Source.Size ?? oldEdge.Source.Size,
            newEdge.Source.VSize ?? oldEdge.Source.VSize,
            newEdge.Source.Weight ?? oldEdge.Source.Weight,
            newEdge.Source.LockTime ?? oldEdge.Source.LockTime);

        var target = new TxNode(
            newEdge.Target.Txid,
            newEdge.Target.Version ?? oldEdge.Target.Version,
            newEdge.Target.Size ?? oldEdge.Target.Size,
            newEdge.Target.VSize ?? oldEdge.Target.VSize,
            newEdge.Target.Weight ?? oldEdge.Target.Weight,
            newEdge.Target.LockTime ?? oldEdge.Target.LockTime);

        return new T2TEdge(
            source, target,
            newEdge.Value,
            newEdge.Type,
            newEdge.Timestamp,
            newEdge.BlockHeight);
    }
}
