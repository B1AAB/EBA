using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Block : BlockMetadata
{
    [JsonPropertyName("tx")]
    public List<Transaction> Transactions { init; get; } = [];

    public ConcurrentDictionary<string, Utxo> TxoLifecycle { init; get; } = [];

    public override DescriptiveStatistics InputCounts { get { return new DescriptiveStatistics([.. _inputsCounts]); } }
    public override DescriptiveStatistics OutputCounts { get { return new DescriptiveStatistics([.. _standardOutputsCounts]); } }
    public override DescriptiveStatistics InputValues { get { return new DescriptiveStatistics([.. _inputValues]); } }
    public override DescriptiveStatistics OutputValues { get { return new DescriptiveStatistics([.. _outputValues]); } }
    public override DescriptiveStatistics SpentOutputAge { get { return new DescriptiveStatistics([.. _spentOutputsAge]); } }

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

    private readonly ConcurrentBag<double> _inputsCounts = [];
    public void AddInputsCount(int value)
    {
        _inputsCounts.Add(value);
    }

    private readonly ConcurrentBag<int> _standardOutputsCounts = [];
    public void AddStandardOutputPerTxCount(int value)
    {
        _standardOutputsCounts.Add(value);
    }

    private readonly ConcurrentBag<long> _inputValues = [];
    public void AddInputValue(long value)
    {
        _inputValues.Add(value);
    }

    private readonly ConcurrentBag<long> _outputValues = [];
    public void AddOutputValue(long value)
    {
        _outputValues.Add(value);
    }

    private readonly ConcurrentBag<long> _spentOutputsAge = [];
    public void AddSpentOutputsAge(long age)
    {
        _spentOutputsAge.Add(age);
    }

    private readonly ConcurrentBag<string> _outputAddresses = [];

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

    public void AddOutputStatistics(string? address, ScriptType scriptType)
    {
        if (!string.IsNullOrEmpty(address))
            _outputAddresses.Add(address);

        _scriptTypeCount.AddOrUpdate(scriptType, 0, (k, v) => v + 1);
    }

    // TODO: this seems to be a bug, you should not added to the same property as other edge types?!
    public void AddNonTransferOutputStatistics(ScriptType scriptType)
    {
        _scriptTypeCount.AddOrUpdate(scriptType, 0, (k, v) => v + 1);
    }

    // TODO: experimental 
    public List<string> ToStringsAddresses(char delimiter)
    {
        var strings = new List<string>();
        foreach (var x in _outputAddresses)
            strings.Add($"{x}{delimiter}{Height}");

        return strings;
    }
}
