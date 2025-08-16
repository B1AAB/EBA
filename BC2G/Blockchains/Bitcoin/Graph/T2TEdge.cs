namespace BC2G.Blockchains.Bitcoin.Graph;

public class T2TEdge : T2TEdge<ContextBase>
{
    public T2TEdge(TxNode<ContextBase> source,
        TxNode<ContextBase> target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight) : base(
            source,
            target,
            value,
            type,
            timestamp,
            blockHeight)
    { }
}

public class T2TEdge<T> : Edge<TxNode<T>, TxNode<T>, T>
    where T : IContext
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinT2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinT2T;
    }

    public EdgeLabel Label { get { return _label; } }
    private readonly EdgeLabel _label;

    public T2TEdge(
        TxNode<T> source, TxNode<T> target,
        long value, EdgeType type, uint timestamp, long blockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        _label = Type == EdgeType.Transfers ? EdgeLabel.T2TTransfer : EdgeLabel.T2TFee;
    }

    public static T2TEdge<T> Update(T2TEdge<T> oldEdge, T2TEdge<T> newEdge)
    {
        var source = new TxNode<T>(
            newEdge.Source.Id,
            newEdge.Source.Txid,
            newEdge.Source.Version ?? oldEdge.Source.Version,
            newEdge.Source.Size ?? oldEdge.Source.Size,
            newEdge.Source.VSize ?? oldEdge.Source.VSize,
            newEdge.Source.Weight ?? oldEdge.Source.Weight,
            newEdge.Source.LockTime ?? oldEdge.Source.LockTime,
            newEdge.Source.Context ?? oldEdge.Source.Context);

        var target = new TxNode<T>(
            newEdge.Target.Id,
            newEdge.Target.Txid,
            newEdge.Target.Version ?? oldEdge.Target.Version,
            newEdge.Target.Size ?? oldEdge.Target.Size,
            newEdge.Target.VSize ?? oldEdge.Target.VSize,
            newEdge.Target.Weight ?? oldEdge.Target.Weight,
            newEdge.Target.LockTime ?? oldEdge.Target.LockTime,
            newEdge.Target.Context ?? oldEdge.Target.Context);

        return new T2TEdge<T>(
            source, target,
            newEdge.Value,
            newEdge.Type,
            newEdge.Timestamp,
            newEdge.BlockHeight);
    }

    public T2TEdge<T> Update(long value)
    {
        return new T2TEdge<T>(Source, Target, Value + value, Type, Timestamp, BlockHeight);
    }
}
