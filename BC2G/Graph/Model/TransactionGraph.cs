namespace BC2G.Graph.Model;

public class TransactionGraph : GraphBase
{
    public double TotalInputValue { set; get; }
    public double TotalOutputValue { set; get; }
    public double Fee { set; get; }

    public ConcurrentDictionary<string, int> SourceTxes { set; get; } = new();
    public ConcurrentDictionary<ScriptNode, double> Sources { set; get; } = new();
    public ConcurrentDictionary<ScriptNode, double> Targets { set; get; } = new();

    public TxNode TxNode { get; }

    public TransactionGraph(Transaction tx) : base()
    {
        TxNode = new TxNode(tx);
    }

    public ScriptNode AddSource(string txid, ScriptNode source, double value)
    {
        SourceTxes.AddOrUpdate(txid, 1, (_, oldValue) => oldValue++);
        TotalInputValue += value;
        return AddOrUpdate(Sources, source, value);
    }

    public ScriptNode AddTarget(ScriptNode target, double value)
    {
        TotalOutputValue += value;
        return AddOrUpdate(Targets, target, value);
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
