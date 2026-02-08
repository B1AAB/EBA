using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class TxGraph(Tx tx) : GraphBase()
{
    public new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinTxGraph; }
    }

    public TxNode TxNode { get; } = new TxNode(tx);

    // TODO: it should be possible to remove this one too
    public long TotalInputValue { get { return _totalInputValue; } }
    private long _totalInputValue;

    public long Fee { set; get; }

    public ConcurrentDictionary<string, long> SourceTxes { set; get; } = new();

    public ReadOnlyCollection<Input> SourceScripts { get { return _sourceScripts.AsReadOnly(); } }
    private readonly List<Input> _sourceScripts = [];

    public ReadOnlyCollection<Output> TargetScripts { get { return _targetScripts.AsReadOnly(); } }
    private readonly List<Output> _targetScripts = [];

    public void AddSource(string txid, Input input, Output prevOut)
    {
        SourceTxes.AddOrUpdate(txid, prevOut.Value, (_, oldValue) => oldValue + prevOut.Value);
        Helpers.ThreadsafeAdd(ref _totalInputValue, prevOut.Value);

        // Note that the Bitcoin protocol allows for multiple inputs with the same scriptSig,
        // so, to keep graph as close to the blockchain as possible,
        // we allow for duplicates in the SourceScripts list, 
        // and any aggregation of inputs with the same scriptSig
        // should be done when sampling the graph for the ML model, not here, 
        // since it provides more flexibility.
        _sourceScripts.Add(input);
    }

    public void AddTarget(Output output)
    {
        // Note that the Bitcoin protocol allows for multiple outputs with the same scriptPubKey,
        // so, to keep graph as close to the blockchain as possible,
        // we allow for duplicates in the TargetScripts list.
        // Any aggregation of outputs with the same scriptPubKey
        // should be done when sampling the graph for the ML model, not here, 
        // since it provides more flexibility. 
        _targetScripts.Add(output);
    }
}
