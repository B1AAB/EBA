using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.GraphModel;

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
    public ConcurrentDictionary<ScriptNode, Utxo> SourceScripts { set; get; } = new();
    public ConcurrentDictionary<ScriptNode, long> TargetScripts { set; get; } = new();
    public ConcurrentDictionary<NonStandardScriptNode, long> TargetNonStandardScripts { set; get; } = new();
    public ConcurrentDictionary<NullDataNode, long> TargetNullDataValues { set; get; } = new();

    public TransactionGraph(Transaction tx) : base()
    {
        TxNode = new TxNode(tx);
    }

    public void AddSource(string txid, Utxo utxo)
    {
        SourceTxes.AddOrUpdate(txid, utxo.Value, (_, oldValue) => oldValue + utxo.Value);
        //RoundedIncrement(ref _totalInputValue, utxo.Value);
        //_totalInputValue += utxo.Value;
        Helpers.ThreadsafeAdd(ref _totalInputValue, utxo.Value);

        SourceScripts.GetOrAdd(new ScriptNode(utxo), utxo);
    }

    public ScriptNode AddTarget(Utxo utxo)
    {
        //RoundedIncrement(ref _totalOutputValue, utxo.Value);
        //_totalOutputValue += utxo.Value;
        Helpers.ThreadsafeAdd(ref _totalOutputValue, utxo.Value);
        return AddOrUpdate(TargetScripts, new ScriptNode(utxo), utxo.Value);
    }

    public void AddTarget(NullDataNode node, long value)
    {
        TargetNullDataValues.AddOrUpdate(node, value, (_, oldValue) => oldValue + value);
    }

    public void AddTarget(NonStandardScriptNode node, long value)
    {
        TargetNonStandardScripts.AddOrUpdate(node, value, (_, oldValue) => oldValue + value);
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
