using BC2G.Utilities;

namespace BC2G.Blockchains.Bitcoin.Graph;

public class TransactionGraph : GraphBase
{
    public new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinTxGraph; }
    }

    public TxNode TxNode { get; }

    public long TotalInputValue { get { return _totalInputValue; } }
    private long _totalInputValue;

    public long TotalOutputValue { get { return _totalOutputValue; } }
    private long _totalOutputValue;

    public long Fee { set; get; }

    public ConcurrentDictionary<string, long> SourceTxes { set; get; } = new();
    public ConcurrentDictionary<ScriptNode, long> SourceScripts { set; get; } = new();
    public ConcurrentDictionary<ScriptNode, long> TargetScripts { set; get; } = new();

    public TransactionGraph(Transaction tx) : base()
    {
        TxNode = new TxNode(tx);
    }

    public ScriptNode AddSource(string txid, Utxo utxo)
    {
        SourceTxes.AddOrUpdate(txid, utxo.Value, (_, oldValue) => oldValue + utxo.Value);
        //RoundedIncrement(ref _totalInputValue, utxo.Value);
        //_totalInputValue += utxo.Value;
        Helpers.ThreadsafeAdd(ref _totalInputValue, utxo.Value);
        return AddOrUpdate(SourceScripts, new ScriptNode(utxo), utxo.Value);
    }

    public ScriptNode AddTarget(Utxo utxo)
    {
        //RoundedIncrement(ref _totalOutputValue, utxo.Value);
        //_totalOutputValue += utxo.Value;
        Helpers.ThreadsafeAdd(ref _totalOutputValue, utxo.Value);
        return AddOrUpdate(TargetScripts, new ScriptNode(utxo), utxo.Value);
    }

    /*
    private static void RoundedIncrement(ref double value, double increment)
    {
        value = Helpers.Round(value + increment);
    }*/

    private static ScriptNode AddOrUpdate(
        ConcurrentDictionary<ScriptNode, long> collection,
        ScriptNode node,
        long value)
    {
        collection.AddOrUpdate(node, value, (_, oldValue) => oldValue + value);
        return node;
    }
}
