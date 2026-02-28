namespace EBA.Blockchains.Bitcoin.GraphModel;

public class S2TEdge : Edge<ScriptNode, TxNode>
{
    public static new EdgeKind Kind => new(ScriptNode.Kind, TxNode.Kind, RelationType.Redeems);

    public long UTxOCreatedInBlockHeight { get; }

    public List<SpentUtxo> SpentUTxOs { get; } = [];

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        uint timestamp,
        long blockHeight,
        List<SpentUtxo> spentUTxOs) :
        base(source, target, spentUTxOs.Sum(x => x.Value), Kind.Relation, timestamp, blockHeight)
    {
        SpentUTxOs = spentUTxOs;
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
            u.Timestamp,
            u.BlockHeight,
            [.. u.SpentUTxOs, .. v.SpentUTxOs]);
    }

    // TODO: maybe a better alternative is to override the base or get from it but now that is static
    public static new string[] GetFeaturesName()
    {
        throw new NotImplementedException();
        /*
        return
        [
            nameof(Value),
            nameof(Relation),
            nameof(BlockHeight),
            nameof(UTxOCreatedInBlockHeight),
            "UtxoAgeBlocks"
        ];*/
    }

    public override double[] GetFeatures()
    {
        throw new NotImplementedException();
        /*
        return
        [
            .. base.GetFeatures(),
            UTxOCreatedInBlockHeight,
            BlockHeight - UTxOCreatedInBlockHeight
        ];*/
    }
}
