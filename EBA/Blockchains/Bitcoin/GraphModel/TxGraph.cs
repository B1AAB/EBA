using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class TxGraph(Tx tx) : GraphBase()
{
    public TxNode TxNode { get; } = new TxNode(tx);

    public long TotalInputValue { get { return _totalInputValue; } }
    private long _totalInputValue;

    public long Fee { set; get; }

    public ConcurrentDictionary<string, long> SourceTxes { set; get; } = new();

    public ReadOnlyCollection<Input> Inputs
    {
        get { return new ReadOnlyCollection<Input>([.. _inputs]); }
    }
    private readonly ConcurrentBag<Input> _inputs = [];

    /// <summary>
    /// Number of inputs in the transaction.
    /// </summary>
    public int InputsCount { get { return _inputs.Count; } }

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

        SourceTxes.AddOrUpdate(input.Txid, prevOut.Value, (_, oldValue) => oldValue + prevOut.Value);
        Helpers.ThreadsafeAdd(ref _totalInputValue, prevOut.Value);

        _inputs.Add(input);
    }

    public void AddOutput(Output output)
    {
        _outputs.Add(output);
    }
}
