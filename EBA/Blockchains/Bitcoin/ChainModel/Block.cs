using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Block : BlockMetadata
{
    [JsonPropertyName("tx")]
    public List<Transaction> Transactions { init; get; } = [];

    public ConcurrentDictionary<string, Utxo> TxoLifecycle { init; get; } = [];

    public override DescriptiveStatistics InputCounts { get { return new DescriptiveStatistics([.. _inputsCounts]); } }
    public override DescriptiveStatistics OutputCounts { get { return new DescriptiveStatistics([.. _outputsCounts]); } }
    public override DescriptiveStatistics InputValues { get { return new DescriptiveStatistics([.. _inputValues]); } }
    public override DescriptiveStatistics OutputValues { get { return new DescriptiveStatistics([.. _outputValues]); } }
    public override DescriptiveStatistics SpentOutputAge { get { return new DescriptiveStatistics([.. _spentOutputsAge]); } }

    public override int CoinbaseOutputsCount { init { _coinbaseOutputsCount = value; } get { return _coinbaseOutputsCount; } }
    private int _coinbaseOutputsCount;
    public void SetCoinbaseOutputsCount(int value)
    {
        _coinbaseOutputsCount = value;
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

    private readonly ConcurrentBag<int> _outputsCounts = [];
    public void AddOutputsCount(int value)
    {
        _outputsCounts.Add(value);
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


    public static string GetStatisticsHeader(char delimiter)
    {
        return string.Join(
            delimiter,
            [
                "BlockHeight",
                "Confirmations",
                "MedianTime",
                "Bits",
                "Difficulty",
                "Size",
                "StrippedSize",
                "Weight",
                "TxCount",
                "MintedBitcoins",
                "TransactionFees",

                "CoinbaseOutputsCount",

                "InputsCountsSum",
                "InputsCountsMax",
                "InputsCountsMin",
                "InputsCountsAvg",
                "InputsCountsMedian",
                "InputsCountsVariance",

                "OutputsCountsSum",
                "OutputsCountsMax",
                "OutputsCountsMin",
                "OutputsCountsAvg",
                "OutputsCountsMedian",
                "OutputsCountsVariance",

                "InputsValuesSum",
                "InputsValuesMax",
                "InputsValuesMin",
                "InputsValuesAvg",
                "InputsValuesMedian",
                "InputsValuesVariance",

                "OutputsValuesSum",
                "OutputsValuesMax",
                "OutputsValuesMin",
                "OutputsValuesAvg",
                "OutputsValuesMedian",
                "OutputsValuesVariance",

                string.Join(
                    delimiter,
                    Enum.GetValues<ScriptType>().Select(x => $"ScriptType_{x}")),

                string.Join(
                    delimiter,
                    Enum.GetValues<EdgeLabel>().Select(
                        x => "BlockGraph" + x + "EdgeCount").ToArray()),
                string.Join(
                    delimiter,
                    Enum.GetValues<EdgeLabel>().Select(
                        x => "BlockGraph" + x + "EdgeValueSum").ToArray()),

                "SpentOutputAgeMax",
                "SpentOutputAgeMin",
                "SpentOutputAgeAvg",
                "SpentOutputAgeMedian",
                "SpentOutputAgeVariance"
            ]);
    }
    public string GetStatistics(char delimiter)
    {
        var insCounts = _inputsCounts.DefaultIfEmpty();
        var outsCounts = _outputsCounts.DefaultIfEmpty();

        var inValues = _inputValues.DefaultIfEmpty();
        var outValues = _outputValues.DefaultIfEmpty();

        var spentTxo = _spentOutputsAge.DefaultIfEmpty();

        return string.Join(
            delimiter,
            [
                Height.ToString(),
                Confirmations.ToString(),
                MedianTime.ToString(),
                Bits,
                Difficulty.ToString(),
                Size.ToString(),
                StrippedSize.ToString(),
                Weight.ToString(),
                TransactionsCount.ToString(),
                Helpers.Satoshi2BTC(MintedBitcoins).ToString(),
                Helpers.Satoshi2BTC(TxFees).ToString(),

                CoinbaseOutputsCount.ToString(),

                insCounts.Sum().ToString(),
                insCounts.Max().ToString(),
                insCounts.Min().ToString(),
                insCounts.Average().ToString(),
                Helpers.GetMedian(insCounts).ToString(),
                Helpers.GetVariance(insCounts).ToString(),

                outsCounts.Sum().ToString(),
                outsCounts.Max().ToString(),
                outsCounts.Min().ToString(),
                outsCounts.Average().ToString(),
                Helpers.GetMedian(outsCounts).ToString(),
                Helpers.GetVariance(outsCounts).ToString(),

                Helpers.Satoshi2BTC(inValues.Sum()).ToString(),
                Helpers.Satoshi2BTC(inValues.Max()).ToString(),
                Helpers.Satoshi2BTC(inValues.Min()).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(inValues.Average())).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(Helpers.GetMedian(inValues))).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(Helpers.GetVariance(inValues))).ToString(),

                Helpers.Satoshi2BTC(outValues.Sum()).ToString(),
                Helpers.Satoshi2BTC(outValues.Max()).ToString(),
                Helpers.Satoshi2BTC(outValues.Min()).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(outValues.Average())).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(Helpers.GetMedian(outValues))).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(Helpers.GetVariance(outValues))).ToString(),

                string.Join(
                    delimiter,
                    Enum.GetValues<ScriptType>().Cast<ScriptType>().Select(e => _scriptTypeCount[e])),

                /*
                string.Join(
                    delimiter,
                    _edgeLabelCount.Select((v, i) => v.ToString()).ToArray()),

                string.Join(
                    delimiter,
                    _edgeLabelValueSum.Select((v, i) => Helpers.Satoshi2BTC(v).ToString()).ToArray()),*/

                spentTxo.Max().ToString(),
                spentTxo.Min().ToString(),
                spentTxo.Average().ToString(),
                Helpers.GetMedian(spentTxo).ToString(),
                Helpers.GetVariance(spentTxo).ToString(),
            ]);
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
