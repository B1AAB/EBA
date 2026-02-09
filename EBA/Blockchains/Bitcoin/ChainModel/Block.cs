using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Block : BlockMetadata
{
    [JsonPropertyName("tx")]
    public List<Tx> Transactions { init; get; } = [];

    public ConcurrentDictionary<string, Utxo> TxoLifecycle { init; get; } = [];

    public override DescriptiveStatistics InputCounts { get { return new DescriptiveStatistics([.. _inputsCounts]); } }
    private readonly ConcurrentBag<int> _inputsCounts = [];

    public override DescriptiveStatistics OutputCounts { get { return new DescriptiveStatistics([.. _outputsCounts]); } }
    private readonly ConcurrentBag<int> _outputsCounts = [];

    public override DescriptiveStatistics InputValues { get { return new DescriptiveStatistics([.. _inputValues]); } }
    private readonly ConcurrentBag<long> _inputValues = [];

    public override DescriptiveStatistics OutputValues { get { return new DescriptiveStatistics([.. _outputValues]); } }
    private readonly ConcurrentBag<long> _outputValues = [];

    public override DescriptiveStatistics SpentOutputAge { get { return new DescriptiveStatistics([.. _spentOutputsAge]); } }
    private readonly ConcurrentBag<long> _spentOutputsAge = [];

    public override DescriptiveStatistics Fees { get { return new DescriptiveStatistics([.. _fees]); } }
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

    public void ProfileSpentOutput(Output prevOut, long prevOutHeight)
    {
        _inputValues.Add(prevOut.Value);
        _inputScriptTypeCount[prevOut.ScriptPubKey.ScriptType] += 1;
        _inputScriptTypeValue[prevOut.ScriptPubKey.ScriptType] += prevOut.Value;
        _spentOutputsAge.Add(Height - prevOutHeight);
    }

    public void ProfileCreatedOutput(Output output)
    {
        _outputValues.Add(output.Value);
        _outputScriptTypeCount[output.ScriptPubKey.ScriptType] += 1;
        _outputScriptTypeValue[output.ScriptPubKey.ScriptType] += output.Value;
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
