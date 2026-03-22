using EBA.Utilities;
using NBitcoin;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class TxGraph(Tx tx) : GraphBase()
{
    public TxNode TxNode { get; } = new TxNode(tx);

    public long TotalInputValue { get { return _totalInputValue; } }
    private long _totalInputValue;

    public long Fee { set; get; }

    public ConcurrentDictionary<string, long> SourceTxes { set; get; } = new();

    public ReadOnlyDictionary<ScriptPubKey, List<Input>> InputScripts
    {
        get { return new ReadOnlyDictionary<ScriptPubKey, List<Input>>(_inputScripts__Old); }
    }
    private readonly ConcurrentDictionary<ScriptPubKey, List<Input>> _inputScripts__Old = new();

    public ReadOnlyCollection<Input> Inputs
    {
        get { return new ReadOnlyCollection<Input>([.. _inputs]); }
    }
    private readonly ConcurrentBag<Input> _inputs = [];

    /// <summary>
    /// Number of inputs in the transaction.
    /// </summary>
    public int InputsCount { get { return _inputScripts__Old.Values.Sum(x => x.Count); } }

    /// <summary>
    /// Unique ScriptPubKeys count in the inputs of the transaction. 
    /// Note that a transaction can have multiple inputs with the same ScriptPubKey, 
    /// so this is not necessarily equal to the total number of inputs.
    /// </summary>
    public int InputScriptsCount { get { return _inputScripts__Old.Keys.Count; } }

    public ReadOnlyCollection<Output> Outputs
    {
        get { return new ReadOnlyCollection<Output>([.. _outputs]); }
    }
    private readonly ConcurrentBag<Output> _outputs = [];

    /// <summary>
    /// Number of outputs in the transaction.
    /// </summary>
    public int OutputsCount { get { return _outputs.Count; } }

    public void AddInput(Input input)
    {
        var prevOut = input.PrevOut;

        SourceTxes.AddOrUpdate(input.TxId, prevOut.Value, (_, oldValue) => oldValue + prevOut.Value);
        Helpers.ThreadsafeAdd(ref _totalInputValue, prevOut.Value);

        _inputs.Add(input);
    }

    public void AddOutput(Output output)
    {
        _outputs.Add(output);
    }
}
