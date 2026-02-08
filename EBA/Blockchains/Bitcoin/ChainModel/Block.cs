using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Block : BlockMetadata
{
    [JsonPropertyName("tx")]
    public List<Tx> Transactions { init; get; } = [];

    public ConcurrentDictionary<string, Utxo> TxoLifecycle { init; get; } = [];

    public override DescriptiveStatistics InputCounts { get { return new DescriptiveStatistics([.. _inputsCounts]); } }
    public override DescriptiveStatistics OutputCounts { get { return new DescriptiveStatistics([.. _outputsCounts]); } }
    public override DescriptiveStatistics InputValues { get { return new DescriptiveStatistics([.. _inputValues]); } }
    public override DescriptiveStatistics OutputValues { get { return new DescriptiveStatistics([.. _outputValues]); } }
    public override DescriptiveStatistics SpentOutputAge { get { return new DescriptiveStatistics([.. _spentOutputsAge]); } }


    private readonly ConcurrentBag<int> _inputsCounts = [];
    private readonly ConcurrentBag<int> _outputsCounts = [];
    private readonly ConcurrentBag<long> _inputValues = [];
    private readonly ConcurrentBag<long> _outputValues = [];
    private readonly ConcurrentBag<long> _spentOutputsAge = [];
    private readonly ConcurrentDictionary<ScriptType, uint> _inputScriptTypeCount = GetEmptyScriptDict();
    private readonly ConcurrentDictionary<ScriptType, uint> _outputScriptTypeCount = GetEmptyScriptDict();
    private static ConcurrentDictionary<ScriptType, uint> GetEmptyScriptDict()
    {
        return new(Enum.GetValues<ScriptType>().Cast<ScriptType>().ToDictionary(x => x, x => (uint)0));
    }

    public override int CoinbaseOutputsCount { init { _coinbaseOutputsCount = value; } get { return _coinbaseOutputsCount; } }
    private int _coinbaseOutputsCount;
    public void SetCoinbaseOutputsCount(int value)
    {
        _coinbaseOutputsCount = value;
    }

    public override long SumNullDataBitcoins
    {
        get => _scriptTypeCount[ScriptType.NullData];
    }
    public override long SumNonStandardOutputBitcoins
    {
        get => _scriptTypeCount[ScriptType.nonstandard];
    }

    public override long TxFees { init { _txFees = value; } get { return _txFees; } }
    private long _txFees;
    public void SetTxFees(long value)
    {
        _txFees = value;
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
        _spentOutputsAge.Add(Height - prevOutHeight);
    }

    public void ProfileCreatedOutput(Output output)
    {
        _outputValues.Add(output.Value);
        _outputScriptTypeCount[output.ScriptPubKey.ScriptType] += 1;
    }

    public void ProfileTxes(int inputsCount, int outputsCount)
    {
        _inputsCounts.Add(inputsCount);
        _outputsCounts.Add(outputsCount);
    }    

    private readonly ConcurrentDictionary<ScriptType, uint> _scriptTypeCount = 
        new(Enum.GetValues<ScriptType>()
                .Cast<ScriptType>()
                .ToDictionary(x => x, x => (uint)0));
    public override Dictionary<ScriptType, uint> ScriptTypeCount
    {
        get
        {
            return _scriptTypeCount.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
