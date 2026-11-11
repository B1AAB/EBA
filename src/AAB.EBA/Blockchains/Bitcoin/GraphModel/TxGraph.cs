namespace AAB.EBA.Blockchains.Bitcoin.GraphModel;

public class TxGraph(Tx tx) : GraphBase
{
    public TxNode TxNode { get; } = new TxNode(tx);

    // important TODO:
    // must add a unit test to compare the sum of inputs to a tx node as built in the BlockGraph equals the TxNode.TotalInputValue property.
    // Similarly, built a test for the sum of outputs to a tx node as built in the BlockGraph equals the TxNode.TotalOutputValue property.
    // Currently they should be equal because the method is not skipping any inputs or outputs as parsed from the Bitcoin blockchain;
    // but if that changes in the future, then the sum of inputs and outputs as built in the BlockGraph may not equal the
    // TxNode.TotalInputValue and TxNode.TotalOutputValue properties, which can lead to inconsistencies. 
    /*
    public long TotalInputValue { get { return _totalInputValue; } }
    private long _totalInputValue;

    public long TotalOutputValue { get { return _totalOutputValue; } }
    private long _totalOutputValue;*/

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
        //Helpers.ThreadsafeAdd(ref _totalInputValue, prevOut.Value);

        _inputs.Add(input);
    }

    public void AddOutput(Output output)
    {
        //Helpers.ThreadsafeAdd(ref _totalOutputValue, output.Value);
        _outputs.Add(output);
    }
}
