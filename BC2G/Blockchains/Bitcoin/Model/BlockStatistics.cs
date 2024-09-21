using BC2G.Utilities;

using System;

namespace BC2G.Blockchains.Bitcoin.Model;

public class BlockStatistics(Block block)
{
    public long Height { get; } = block.Height;
    public int Confirmations { get; } = block.Confirmations;
    public string Bits { get; } = block.Bits;
    public double Difficulty { get; } = block.Difficulty;
    public int Size { get; } = block.Size;
    public int StrippedSize { get; } = block.StrippedSize;
    public int Weight { get; } = block.Weight;
    public int TransactionsCount { get; } = block.TransactionsCount;

    public double MintedBitcoins { set; get; }
    public double TxFees { set; get; }

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

    private readonly ConcurrentBag<double> _inputValues = [];
    private readonly ConcurrentBag<double> _outputValues = [];

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

    public Dictionary<EdgeLabel, double> EdgeLabelValueSum
    {
        get
        {
            return
                _edgeLabelValueSum
                .Select((v, i) => new { Key = (EdgeLabel)i, Value = v })
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private readonly double[] _edgeLabelValueSum =
        new double[Enum.GetNames(typeof(EdgeLabel)).Length];

    private readonly Dictionary<ScriptType, uint> _scriptTypeCount = 
        Enum.GetValues(typeof(ScriptType))
            .Cast<ScriptType>()
            .ToDictionary(x => x, x => (uint)0);

    private const char _delimiter = '\t';

    public void StartStopwatch()
    {
        _stopwatch.Start();
    }
    public void StopStopwatch()
    {
        _stopwatch.Stop();
    }

    public void IncrementEdgeType(EdgeLabel label, double value)
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

    public void AddInputValue(double value)
    {
        _inputValues.Add(value);
    }
    public void AddOutputValue(double value)
    {
        _outputValues.Add(value);
    }

    public void AddOutputAddress(string? address)
    {
        if (!string.IsNullOrEmpty(address))
            _outputAddresses.Add(address);
    }

    public static string GetHeader()
    {
        return string.Join(
            _delimiter,
            [
                "BlockHeight",
                "Runtime(seconds)",
                "Confirmations",
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

                string.Join(
                    _delimiter,
                    ((EdgeLabel[])Enum.GetValues(typeof(EdgeLabel))).Select(
                        x => "BlockGraph" + x + "EdgeCount").ToArray()),
                string.Join(
                    _delimiter,
                    ((EdgeLabel[])Enum.GetValues(typeof(EdgeLabel))).Select(
                        x => "BlockGraph" + x + "EdgeValueSum").ToArray()),
            ]);
    }

    public override string ToString()
    {
        return string.Join(
            _delimiter,
            [
                Height.ToString(),
                Runtime.TotalSeconds.ToString(),
                Confirmations.ToString(),
                Bits,
                Difficulty.ToString(),
                Size.ToString(),
                StrippedSize.ToString(),
                Weight.ToString(),
                Retries.ToString(),
                TransactionsCount.ToString(),
                MintedBitcoins.ToString(),
                TxFees.ToString(),

                CoinbaseOutputsCount.ToString(),

                _inputsCounts.Sum().ToString(),
                _inputsCounts.Max().ToString(),
                _inputsCounts.Min().ToString(),
                _inputsCounts.Average().ToString(),
                Helpers.GetMedian(_inputsCounts).ToString(),
                Helpers.GetVariance(_inputsCounts).ToString(),

                _outputsCounts.Sum().ToString(),
                _outputsCounts.Max().ToString(),
                _outputsCounts.Min().ToString(),
                _outputsCounts.Average().ToString(),
                Helpers.GetMedian(_outputsCounts).ToString(),
                Helpers.GetVariance(_outputsCounts).ToString(),

                _inputValues.Sum().ToString(),
                _inputValues.Max().ToString(),
                _inputValues.Min().ToString(),
                _inputValues.Average().ToString(),
                Helpers.GetMedian(_inputValues).ToString(),
                Helpers.GetVariance(_inputValues).ToString(),

                _outputValues.Sum().ToString(),
                _outputValues.Max().ToString(),
                _outputValues.Min().ToString(),
                _outputValues.Average().ToString(),
                Helpers.GetMedian(_outputValues).ToString(),
                Helpers.GetVariance(_outputValues).ToString(),

                string.Join(
                    _delimiter,
                    _edgeLabelCount.Select((v, i) => v.ToString()).ToArray()),

                string.Join(
                    _delimiter,
                    _edgeLabelValueSum.Select((v, i) => v.ToString()).ToArray()),

                Environment.NewLine
            ]);
    }

    public static string GetHeaderAddresses()
    {
        return string.Join(_delimiter, ["BlockHeight", "OutputAddresses"]);
    }

    public string ToStringAddressess()
    {
        return string.Join(
            _delimiter,
            [
                Height.ToString(),
                string.Join(";", _outputAddresses),
                Environment.NewLine
            ]);
    }
}
