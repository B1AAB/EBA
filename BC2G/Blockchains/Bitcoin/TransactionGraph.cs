namespace BC2G.Graph.Model;

public class TransactionGraph : GraphBase
{
    public TxNode TxNode { get; }
    public double TotalInputValue { set; get; }
    public double TotalOutputValue { set; get; }
    public double Fee { set; get; }

    public ConcurrentDictionary<string, double> SourceTxes { set; get; } = new();
    public ConcurrentDictionary<ScriptNode, double> SourceScripts { set; get; } = new();
    public ConcurrentDictionary<ScriptNode, double> TargetScripts { set; get; } = new();

    public TransactionGraph(Transaction tx) : base()
    {
        TxNode = new TxNode(tx);
    }

    public ScriptNode AddSource(string txid, string utxoId, string address, ScriptType scriptType, double value)
    {
        SourceTxes.AddOrUpdate(txid, value, (_, oldValue) => oldValue + value);
        TotalInputValue += value;
        return AddOrUpdate(SourceScripts, new ScriptNode(utxoId, address, scriptType), value);
    }

    public ScriptNode AddTarget(string utxoId, string address, ScriptType scriptType, double value)
    {
        TotalOutputValue += value;
        return AddOrUpdate(TargetScripts, new ScriptNode(utxoId, address, scriptType), value);
    }

    private static ScriptNode AddOrUpdate(
        ConcurrentDictionary<ScriptNode, double> collection,
        ScriptNode node,
        double value)
    {
        collection.AddOrUpdate(
            node, Utilities.Round(value),
            (_, oldValue) => Utilities.Round(oldValue + value));

        return node;
    }
}
