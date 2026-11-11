namespace AAB.EBA.Blockchains.Bitcoin.GraphModel;

public class S2TEdge : Edge<ScriptNode, TxNode>
{
    public static new EdgeKind Kind => new(ScriptNode.Kind, TxNode.Kind, RelationType.Redeems);

    public string Txid { get; }
    public int Vout { get; }
    public bool Generated { get; }
    public long CreationHeight { get; }

    public long SpentHeight { get { return Height; } }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        long spentHeight,
        Input spentUTxO)
        : this(
            source: source,
            target: target,
            spentHeight: spentHeight,
            value: spentUTxO.PrevOut.Value,
            txid: spentUTxO.Txid,
            vout: spentUTxO.Vout,
            generated: spentUTxO.PrevOut.Generated,
            creationHeight: spentUTxO.PrevOut.Height)
    { }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        long spentHeight,
        long value,
        string txid,
        int vout,
        bool generated,
        long creationHeight) :
        base(
            source: source,
            target: target,
            value: value,
            relation: Kind.Relation,
            height: spentHeight)
    {
        Txid = txid;
        Vout = vout;
        Generated = generated;
        CreationHeight = creationHeight;
    }

    public static new string[] GetFeaturesName()
    {
        return
        [
            .. Edge<ScriptNode, TxNode>.GetFeaturesName(), 
            nameof(Generated),
            nameof(CreationHeight),
            nameof(SpentHeight)
        ];
    }

    public override double[] GetFeatures()
    {
        return
        [
            .. base.GetFeatures(),
            Generated ? 1.0 : 0.0,
            CreationHeight,
            SpentHeight,
        ];
    }

    // Each S2T edge represents redeeming one specific UTxO, identified by (Txid, Vout).
    // Without these in the equality, two inputs of the same Tx that spend
    // different UTxOs of the same script with the same value would be considered
    // equal, and the second would be silently dropped when added to the graph.
    // 
    // one example is at the following block: 100002
    // txid: 220ebc64e21abece964927322cba69180ed853bb187fbc6923bac7d010b9d87a
    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Txid, Vout);
    }

    public override bool Equals(object? obj)
    {
        return obj is S2TEdge other
               && Equals(other)
               && Txid == other.Txid
               && Vout == other.Vout;
    }
}
