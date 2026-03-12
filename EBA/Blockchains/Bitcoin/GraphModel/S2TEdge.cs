namespace EBA.Blockchains.Bitcoin.GraphModel;

public class S2TEdge : Edge<ScriptNode, TxNode>
{
    public static new EdgeKind Kind => new(ScriptNode.Kind, TxNode.Kind, RelationType.Redeems);

    public int SpentUTxOsCount { get { return SpentUTxOs.Count; } }

    public List<SpentUTxO> SpentUTxOs { get; } = [];

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        uint timestamp,
        long blockHeight,
        List<SpentUTxO> spentUTxOs) :
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

    public static new string[] GetFeaturesName()
    {
        return
        [
            .. Edge<ScriptNode, TxNode>.GetFeaturesName(),

            nameof(SpentUTxOsCount),
            "SpentUTxOsValueMin",
            "SpentUTxOsValueMax",
            "SpentUTxOsValueAvg",
            "SpentUTxOsAgeMin",
            "SpentUTxOsAgeMax",
            "SpentUTxOsAgeAvg",
            "SpentUTxOsGeneratedCount"
        ];
    }

    public override double[] GetFeatures()
    {
        return
        [
            .. base.GetFeatures(),
            SpentUTxOsCount,
            SpentUTxOs.Min(x => x.Value),
            SpentUTxOs.Max(x => x.Value),
            SpentUTxOs.Average(x => x.Value),
            SpentUTxOs.Min(x => x.Height),
            SpentUTxOs.Max(x => x.Height),
            SpentUTxOs.Average(x => x.Height),
            SpentUTxOs.Count(x => x.Generated)
        ];
    }
}
