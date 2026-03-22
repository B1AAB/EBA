namespace EBA.Blockchains.Bitcoin.GraphModel;

public class S2TEdge : Edge<ScriptNode, TxNode>
{
    public static new EdgeKind Kind => new(ScriptNode.Kind, TxNode.Kind, RelationType.Redeems);

    public string TxId { get; }
    public int Vout { get; }
    public bool Generated { get; }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        uint timestamp,
        long blockHeight,
        Input spentUTxO)
        : this(
            source: source,
            target: target,
            timestamp: timestamp,
            blockHeight: blockHeight,
            value: spentUTxO.PrevOut.Value,
            txid: spentUTxO.TxId,
            vout: spentUTxO.Vout,
            generated: spentUTxO.PrevOut.Generated)
    { }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        uint timestamp,
        long blockHeight,
        long value,
        string txid,
        int vout,
        bool generated) :
        base(source, target, value, Kind.Relation, timestamp, blockHeight)
    {
        TxId = txid;
        Vout = vout;
        Generated = generated;
    }
}
