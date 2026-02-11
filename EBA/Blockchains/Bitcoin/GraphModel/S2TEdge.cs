namespace EBA.Blockchains.Bitcoin.GraphModel;

public class S2TEdge : Edge<ScriptNode, TxNode>
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2T;
    }

    public long UTxOCreatedInBlockHeight { get; }

    public List<PrevOut> PrevOuts { get; } = [];

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight,
        long utxoCreatedInBlockHeight) :
        base(source, target, value, type, EdgeLabel.S2TTransfer, timestamp, blockHeight)
    {
        UTxOCreatedInBlockHeight = utxoCreatedInBlockHeight;
    }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        EdgeType type,
        uint timestamp,
        long blockHeight,
        List<PrevOut> prevOuts) :
        base(source, target, prevOuts.Sum(x => x.Value), type, EdgeLabel.S2TTransfer, timestamp, blockHeight)
    {
        PrevOuts = prevOuts;
    }

    /// <summary>
    /// Use this method when you're sure the two edges are identical 
    /// (e.g., same source and destination) to merge only their outputs list 
    /// and sum of value.
    /// </summary>
    public static S2TEdge Merge(S2TEdge u, S2TEdge v)
    {
        return new S2TEdge(
            u.Source,
            u.Target,
            u.Type,
            u.Timestamp,
            u.BlockHeight,
            [.. u.PrevOuts, .. v.PrevOuts]);
    }

    // TODO: maybe a better alternative is to override the base or get from it but now that is static
    public static new string[] GetFeaturesName()
    {
        return
        [
            nameof(Value),
            nameof(Type),
            nameof(BlockHeight),
            nameof(UTxOCreatedInBlockHeight),
            "UtxoAgeBlocks"
        ];
    }

    public override double[] GetFeatures()
    {
        return
        [
            .. base.GetFeatures(),
            UTxOCreatedInBlockHeight,
            BlockHeight - UTxOCreatedInBlockHeight
        ];
    }
}
