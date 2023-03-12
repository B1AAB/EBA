namespace BC2G.Blockchains.Bitcoin.Graph;

public class TransactionGraph : GraphBase
{
    public new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinTxGraph; }
    }

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

    public ScriptNode AddSource(string txid, Utxo utxo)
    {
        SourceTxes.AddOrUpdate(txid, utxo.Value, (_, oldValue) => oldValue + utxo.Value);
        TotalInputValue += utxo.Value;
        return AddOrUpdate(SourceScripts, new ScriptNode(utxo), utxo.Value);
    }

    public ScriptNode AddTarget(Utxo utxo)
    {
        TotalOutputValue += utxo.Value;
        return AddOrUpdate(TargetScripts, new ScriptNode(utxo), utxo.Value);
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
