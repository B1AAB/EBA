using BC2G.Utilities;

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

    /// <summary>
    /// Sets and gets retry attempts to contruct the block graph.
    /// </summary>
    public int Retries { set; get; } = 0;

    public TimeSpan Runtime { get { return _stopwatch.Elapsed; } }
    private readonly Stopwatch _stopwatch = new();

    public int InputTxCountsSum { get { return _inputTxCounts.Sum(); } }
    private readonly ConcurrentBag<int> _inputTxCounts = [];

    public int OutputTxCountsSum { get { return _outputTxCounts.Sum(); } }
    private readonly ConcurrentBag<int> _outputTxCounts = [];

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

    public void AddInputTxCount(int value)
    {
        _inputTxCounts.Add(value);
    }
    public void AddOutputTxCount(int value)
    {
        _outputTxCounts.Add(value);
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
                "InputTxCountsMin(ExcludingCoinbase)",
                "InputTxCountsMax",
                "InputTxCountsSum",
                "InputTxCountsAvg",
                "InputTxCountsMedian",
                "InputTxCountsVariance",
                "OutputTxCountsMin",
                "OutputTxCountsMax",
                "OutputTxCountsSum",
                "OutputTxCountsAvg",
                "OutputTxCountsMedian",
                "OutputTxCountsVariance",

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
        var inTxExCoinbase = new List<int>(_inputTxCounts);
        inTxExCoinbase.Remove(1);
        var inTxExCMin = inTxExCoinbase.Count > 0 ? inTxExCoinbase.Min().ToString() : "0";

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

                inTxExCMin,
                _inputTxCounts.Max().ToString(),
                _inputTxCounts.Sum().ToString(),
                _inputTxCounts.Average().ToString(),
                Helpers.GetMedian(_inputTxCounts).ToString(),
                Helpers.GetVariance(_inputTxCounts).ToString(),

                _outputTxCounts.Min().ToString(),
                _outputTxCounts.Max().ToString(),
                _outputTxCounts.Sum().ToString(),
                _outputTxCounts.Average().ToString(),
                Helpers.GetMedian(_outputTxCounts).ToString(),
                Helpers.GetVariance(_outputTxCounts).ToString(),

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
