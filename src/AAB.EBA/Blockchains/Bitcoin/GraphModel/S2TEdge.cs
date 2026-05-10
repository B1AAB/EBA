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
}
