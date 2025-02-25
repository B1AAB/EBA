using BC2G.Utilities;

using System;

namespace BC2G.Blockchains.Bitcoin.Model;

public class BlockStatistics(Block block)
{
    public long Height { get; } = block.Height;
    public int Confirmations { get; } = block.Confirmations;
    public uint MedianTime { get; } = block.MedianTime;
    public string Bits { get; } = block.Bits;
    public double Difficulty { get; } = block.Difficulty;
    public int Size { get; } = block.Size;
    public int StrippedSize { get; } = block.StrippedSize;
    public int Weight { get; } = block.Weight;
    public int TransactionsCount { get; } = block.TransactionsCount;

    public long MintedBitcoins { set; get; }
    public long TxFees { set; get; }

    public int CoinbaseOutputsCount { set; get; }

    /// <summary>
    /// Sets and gets retry attempts to contruct the block graph.
    /// </summary>
    public int Retries { set; get; } = 0;

    public TimeSpan Runtime { get { return _stopwatch.Elapsed; } }
    private readonly Stopwatch _stopwatch = new();

    public int InputsCount { get { return _inputsCounts.Sum(); } }
    private readonly ConcurrentBag<int> _inputsCounts = [];

    public int OutputsCount { get { return _outputsCounts.Sum(); } }
    private readonly ConcurrentBag<int> _outputsCounts = [];

    private readonly ConcurrentBag<long> _inputValues = [];
    private readonly ConcurrentBag<long> _outputValues = [];

    private readonly ConcurrentBag<int> _spentOutputsAge = [];

    public List<string> OutputAddresses { get { return [.. _outputAddresses]; } }
    private readonly ConcurrentBag<string> _outputAddresses = [];

    public Dictionary<EdgeLabel, uint> EdgeLabelCount
    {
        get
        {
            return
                _edgeLabelCount
                .Select((v, i) => new { Key = (EdgeLabel)i, Value = v })
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private readonly uint[] _edgeLabelCount =
        new uint[Enum.GetNames(typeof(EdgeLabel)).Length];

    public Dictionary<EdgeLabel, long> EdgeLabelValueSum
    {
        get
        {
            return
                _edgeLabelValueSum
                .Select((v, i) => new { Key = (EdgeLabel)i, Value = v })
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private readonly long[] _edgeLabelValueSum =
        new long[Enum.GetNames(typeof(EdgeLabel)).Length];

    private readonly ConcurrentDictionary<ScriptType, uint> _scriptTypeCount =
        new(Enum.GetValues(typeof(ScriptType))
                .Cast<ScriptType>()
                .ToDictionary(x => x, x => (uint)0));

    private const char _delimiter = '\t';

    public void StartStopwatch()
    {
        _stopwatch.Start();
    }
    public void StopStopwatch()
    {
        _stopwatch.Stop();
    }

    public void IncrementEdgeType(EdgeLabel label, long value)
    {
        Interlocked.Increment(ref _edgeLabelCount[(int)label]);
        Helpers.ThreadsafeAdd(ref _edgeLabelValueSum[(int)label], value);
    }

    public void AddInputsCount(int value)
    {
        _inputsCounts.Add(value);
    }
    public void AddOutputsCount(int value)
    {
        _outputsCounts.Add(value);
    }

    public void AddInputValue(long value)
    {
        _inputValues.Add(value);
    }
    public void AddOutputValue(long value)
    {
        _outputValues.Add(value);
    }

    public void AddSpentOutputsAge(int age)
    {
        _spentOutputsAge.Add(age);
    }

    public void AddOutputStatistics(string? address, ScriptType scriptType)
    {
        if (!string.IsNullOrEmpty(address))
            _outputAddresses.Add(address);

        _scriptTypeCount.AddOrUpdate(scriptType, 0, (k, v) => v + 1);
    }

    public void AddNonTransferOutputStatistics(ScriptType scriptType)
    {
        _scriptTypeCount.AddOrUpdate(scriptType, 0, (k, v) => v + 1);
    }

    public static string GetHeader()
    {
        return string.Join(
            _delimiter,
            [
                "BlockHeight",
                "Runtime(seconds)",
                "Confirmations",
                "MedianTime",
                "Bits",
                "Difficulty",
                "Size",
                "StrippedSize",
                "Weight",
                "Retries",
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

                string.Join(_delimiter,((ScriptType[])Enum.GetValues(typeof(ScriptType))).Select(x => $"ScriptType_{x}")),

                string.Join(
                    _delimiter,
                    ((EdgeLabel[])Enum.GetValues(typeof(EdgeLabel))).Select(
                        x => "BlockGraph" + x + "EdgeCount").ToArray()),
                string.Join(
                    _delimiter,
                    ((EdgeLabel[])Enum.GetValues(typeof(EdgeLabel))).Select(
                        x => "BlockGraph" + x + "EdgeValueSum").ToArray()),

                "SpentOutputAgeMax",
                "SpentOutputAgeMin",
                "SpentOutputAgeAvg",
                "SpentOutputAgeMedian",
                "SpentOutputAgeVariance"
            ]);
    }

    public override string ToString()
    {
        var insCounts = _inputsCounts.DefaultIfEmpty();
        var outsCounts = _outputsCounts.DefaultIfEmpty();

        var inValues = _inputValues.DefaultIfEmpty();
        var outValues = _outputValues.DefaultIfEmpty();

        var spentTxo = _spentOutputsAge.DefaultIfEmpty();

        return string.Join(
            _delimiter,
            [
                Height.ToString(),
                Runtime.TotalSeconds.ToString(),
                Confirmations.ToString(),
                MedianTime.ToString(),
                Bits,
                Difficulty.ToString(),
                Size.ToString(),
                StrippedSize.ToString(),
                Weight.ToString(),
                Retries.ToString(),
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
                    _delimiter,
                    Enum.GetValues(typeof(ScriptType)).Cast<ScriptType>().Select(e => _scriptTypeCount[e])),

                string.Join(
                    _delimiter,
                    _edgeLabelCount.Select((v, i) => v.ToString()).ToArray()),

                string.Join(
                    _delimiter,
                    _edgeLabelValueSum.Select((v, i) => Helpers.Satoshi2BTC(v).ToString()).ToArray()),

                Helpers.Satoshi2BTC(spentTxo.Max()).ToString(),
                Helpers.Satoshi2BTC(spentTxo.Min()).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(spentTxo.Average())).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(Helpers.GetMedian(spentTxo))).ToString(),
                Helpers.Satoshi2BTC(Helpers.Round(Helpers.GetVariance(spentTxo))).ToString(),
            ]);
    }

    public static string GetHeaderAddresses()
    {
        return string.Join(_delimiter, ["BlockHeight", "OutputAddresses"]);
    }

    // TODO: experimental 
    public List<string> ToStringsAddresses()
    {
        var strings = new List<string>();
        foreach(var x in _outputAddresses)
            strings.Add($"{x}{_delimiter}{Height}");

        return strings;
    }
}
