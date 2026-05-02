using AAB.EBA.Utilities;

namespace AAB.EBA.Blockchains.Bitcoin.ChainModel;

public class Block : BlockMetadata
{
    [JsonPropertyName("tx")]
    public List<Tx> Transactions { init; get; } = [];

    public ConcurrentDictionary<string, Utxo> TxoLifecycle { init; get; } = [];

    public override DescriptiveStatistics InputCountsStats { get { return new DescriptiveStatistics([.. _inputsCounts]); } }
    private readonly ConcurrentBag<int> _inputsCounts = [];

    public override DescriptiveStatistics OutputCountsStats { get { return new DescriptiveStatistics([.. _outputsCounts]); } }
    private readonly ConcurrentBag<int> _outputsCounts = [];

    public override DescriptiveStatistics InputValuesStats { get { return new DescriptiveStatistics([.. _inputValues]); } }
    private readonly ConcurrentBag<long> _inputValues = [];

    public override DescriptiveStatistics OutputValuesStats { get { return new DescriptiveStatistics([.. _outputValues]); } }
    private readonly ConcurrentBag<long> _outputValues = [];

    public override DescriptiveStatistics SpentOutputAgeStats { get { return new DescriptiveStatistics([.. _spentOutputsAge]); } }
    private readonly ConcurrentBag<long> _spentOutputsAge = [];

    public override DescriptiveStatistics FeesStats { get { return new DescriptiveStatistics([.. _fees]); } }
    private readonly ConcurrentBag<long> _fees = [];


    public override Dictionary<ScriptType, long> InputScriptTypeCount
    {
        get { return _inputScriptTypeCount.ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
    }
    private readonly ConcurrentDictionary<ScriptType, long> _inputScriptTypeCount = GetEmptyScriptDict();

    public override Dictionary<ScriptType, long> OutputScriptTypeCount
    {
        get { return _outputScriptTypeCount.ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
    }
    private readonly ConcurrentDictionary<ScriptType, long> _outputScriptTypeCount = GetEmptyScriptDict();

    public override Dictionary<ScriptType, long> InputScriptTypeValue
    {
        get { return _inputScriptTypeValue.ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
    }
    private readonly ConcurrentDictionary<ScriptType, long> _inputScriptTypeValue = GetEmptyScriptDict();

    public override Dictionary<ScriptType, long> OutputScriptTypeValue
    {
        get { return _outputScriptTypeValue.ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
    }
    private readonly ConcurrentDictionary<ScriptType, long> _outputScriptTypeValue = GetEmptyScriptDict();

    private static ConcurrentDictionary<ScriptType, long> GetEmptyScriptDict()
    {
        return new(Enum.GetValues<ScriptType>().Cast<ScriptType>().ToDictionary(x => x, x => (long)0));
    }

    public override int CoinbaseOutputsCount { init { _coinbaseOutputsCount = value; } get { return _coinbaseOutputsCount; } }
    private int _coinbaseOutputsCount;
    public void SetCoinbaseOutputsCount(int value)
    {
        _coinbaseOutputsCount = value;
    }

    public override long MintedBitcoins { init { _mintedBitcoins = value; } get { return _mintedBitcoins; } }
    private long _mintedBitcoins;
    public void SetMintedBitcoins(long value)
    {
        _mintedBitcoins = value;
    }

    public void ProfileSpentOutput(ScriptPubKey scriptPubKey, List<Input> inputs)
    {
        long sumValues = 0;
        foreach (var input in inputs)
        {
            _spentOutputsAge.Add(Height - input.PrevOut.Height);
            sumValues += input.PrevOut.Value;
        }

        _inputValues.Add(sumValues);
        _inputScriptTypeCount[scriptPubKey.ScriptType] += 1;
        _inputScriptTypeValue[scriptPubKey.ScriptType] += sumValues;
    }

    public void ProfileSpentOutput(Input input)
    {
        _spentOutputsAge.Add(Height - input.PrevOut.Height);

        _inputValues.Add(input.PrevOut.Value);
        _inputScriptTypeCount[input.PrevOut.ScriptPubKey.ScriptType] += 1;
        _inputScriptTypeValue[input.PrevOut.ScriptPubKey.ScriptType] += input.PrevOut.Value;
    }

    public void ProfileCreatedOutput(Output output)
    {
        _outputValues.Add(output.Value);
        _outputScriptTypeCount[output.ScriptPubKey.ScriptType] += 1;
        _outputScriptTypeValue[output.ScriptPubKey.ScriptType] += output.Value;
    }

    public void ProfileCreatedOutput(ScriptPubKey scriptPubKey, List<Output> outputs)
    {
        long sumValues = outputs.Sum(x => x.Value);

        _outputValues.Add(sumValues);
        _outputScriptTypeCount[scriptPubKey.ScriptType] += 1;
        _outputScriptTypeValue[scriptPubKey.ScriptType] += sumValues;
    }

    public void ProfileTxes(int inputsCount, int outputsCount)
    {
        _inputsCounts.Add(inputsCount);
        _outputsCounts.Add(outputsCount);
    } 
    
    public void ProfileFee(long fee)
    {
        _fees.Add(fee);
    }
}
