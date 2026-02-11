using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class TxGraph(Tx tx) : GraphBase()
{
    public new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinTxGraph; }
    }

    public TxNode TxNode { get; } = new TxNode(tx);

    public long TotalInputValue { get { return _totalInputValue; } }
    private long _totalInputValue;

    public long Fee { set; get; }

    public ConcurrentDictionary<string, long> SourceTxes { set; get; } = new();

    public ReadOnlyDictionary<ScriptPubKey, List<PrevOut>> InputScripts
    {
        get { return new ReadOnlyDictionary<ScriptPubKey, List<PrevOut>>(_inputScripts); }
    }
    private readonly ConcurrentDictionary<ScriptPubKey, List<PrevOut>> _inputScripts = new();

    /// <summary>
    /// Number of inputs in the transaction.
    /// </summary>
    public int InputsCount { get { return _inputScripts.Values.Sum(x => x.Count); } }

    /// <summary>
    /// Unique ScriptPubKeys count in the inputs of the transaction. 
    /// Note that a transaction can have multiple inputs with the same ScriptPubKey, 
    /// so this is not necessarily equal to the total number of inputs.
    /// </summary>
    public int InputScriptsCount { get { return _inputScripts.Keys.Count; } }


    public ReadOnlyDictionary<ScriptPubKey, List<Output>> OutputScripts
    {
        get { return new ReadOnlyDictionary<ScriptPubKey, List<Output>>(_outputScripts); }
    }
    private readonly ConcurrentDictionary<ScriptPubKey, List<Output>> _outputScripts = new();

    /// <summary>
    /// Number of outputs in the transaction.
    /// </summary>
    public int OutputsCount { get { return _outputScripts.Values.Sum(x => x.Count); } }

    /// <summary>
    /// Unique ScriptPubKeys count in the outputs of the transaction.
    /// Note that a transaction can have multiple outputs with the same ScriptPubKey,
    /// so this is not necessarily equal to the total number of outputs.
    /// </summary>
    public int OutputScriptsCount { get { return _outputScripts.Keys.Count; } }

    public void AddInput(string txid, Input input)
    {
        var prevOut = input.PrevOut;

        SourceTxes.AddOrUpdate(txid, prevOut.Value, (_, oldValue) => oldValue + prevOut.Value);
        Helpers.ThreadsafeAdd(ref _totalInputValue, prevOut.Value);

        // dev point:
        // an example block that contains multiple inputs with the same scriptPubKey is 320700
        _inputScripts.AddOrUpdate(
            prevOut.ScriptPubKey,
            [input.PrevOut],
            (_, oldValue) =>
            {
                oldValue.Add(input.PrevOut);
                return oldValue;
            });
    }

    public void AddOutput(Output output)
    {
        // dev point:
        // an example block that contains multiple outputs with the same scriptPubKey is 840000
        _outputScripts.AddOrUpdate(
            output.ScriptPubKey, [output],
            (_, oldValue) =>
            {
                oldValue.Add(output);
                return oldValue;
            });
    }
}
