namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2TEdge : Edge<TxNode, TxNode>
{
    public T2TEdge(
        TxNode source, TxNode target,
        long value, RelationType type, uint timestamp, long height) :
        base(
            source: source,
            target: target,
            value: value,
            relation: type,
            timestamp: timestamp,
            height: height)
    { }

    public static EdgeKind KindTransfers => new(TxNode.Kind, TxNode.Kind, RelationType.Transfers);
    public static EdgeKind KindFee => new(TxNode.Kind, TxNode.Kind, RelationType.Fee);

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
            newEdge.Relation,
            newEdge.Timestamp,
            newEdge.Height);
    }
}
