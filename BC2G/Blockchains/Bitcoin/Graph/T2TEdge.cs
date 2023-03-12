namespace BC2G.Blockchains.Bitcoin.Graph;

public class T2TEdge : Edge<TxNode, TxNode>
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2T; }
    }

    public T2TEdge(
        TxNode source, TxNode target,
        double value, EdgeType type, uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    { }

    public static T2TEdge Update(T2TEdge oldEdge, T2TEdge newEdge)
    {
        var source = new TxNode(
            newEdge.Source.Id,
            newEdge.Source.Txid ?? oldEdge.Source.Txid,
            newEdge.Source.Version ?? oldEdge.Source.Version,
            newEdge.Source.Size ?? oldEdge.Source.Size,
            newEdge.Source.VSize ?? oldEdge.Source.VSize,
            newEdge.Source.Weight ?? oldEdge.Source.Weight,
            newEdge.Source.LockTime ?? oldEdge.Source.LockTime);

        var target = new TxNode(
            newEdge.Target.Id,
            newEdge.Target.Txid ?? oldEdge.Target.Txid,
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

    public T2TEdge Update(double value)
    {
        return new T2TEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
