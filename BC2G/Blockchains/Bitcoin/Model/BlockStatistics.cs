namespace BC2G.Blockchains.Bitcoin.Model;

public class BlockStatistics
{
    public long Height { get; }
    public int Confirmations { get; }
    public string Bits { get; }
    public double Difficulty { get; }
    public int Size { get; }
    public int StrippedSize { get; }
    public int Weight { get; }
    public int TransactionsCount { get; }

    /// <summary>
    /// Sets and gets retry attempts to contruct the block graph.
    /// </summary>
    public int Retries { set; get; } = 0;

    public TimeSpan Runtime { get { return _stopwatch.Elapsed; } }
    private readonly Stopwatch _stopwatch = new();

    public int InputTxCount { get { return _inputTxCount; } }
    private int _inputTxCount;

    public int OutputTxCount { get { return _outputTxCount; } }
    private int _outputTxCount;

    public Dictionary<EdgeType, uint> EdgeTypeFrequency
    {
        get
        {
            return
                _edgeTypeFrequency
                .Select((v, i) => new { Key = (EdgeType)i, Value = v })
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private readonly uint[] _edgeTypeFrequency =
        new uint[Enum.GetNames(typeof(EdgeType)).Length];

    public Dictionary<EdgeType, double> EdgeTypeTxSum
    {
        get
        {
            return
                _edgeTypeTxSum
                .Select((v, i) => new { Key = (EdgeType)i, Value = v })
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private readonly double[] _edgeTypeTxSum = 
        new double[Enum.GetNames(typeof(EdgeType)).Length];

    private const char _delimiter = '\t';

    public BlockStatistics(Block block)
    {
        Height = block.Height;
        Confirmations = block.Confirmations;
        Bits = block.Bits;
        Difficulty = block.Difficulty;
        Size = block.Size;
        StrippedSize = block.StrippedSize;
        Weight = block.Weight;
        TransactionsCount = block.TransactionsCount;
    }

    public void StartStopwatch()
    {
        _stopwatch.Start();
    }
    public void StopStopwatch()
    {
        _stopwatch.Stop();
    }

    public void IncrementEdgeType(EdgeType type, double value)
    {
        Interlocked.Increment(ref _edgeTypeFrequency[(int)type]);
        Utilities.ThreadsafeAdd(ref _edgeTypeTxSum[(int)type], value);
    }

    public void AddInputTxCount(int value)
    {
        Interlocked.Add(ref _inputTxCount, value);
    }
    public void AddOutputTxCount(int value)
    {
        Interlocked.Add(ref _outputTxCount, value);
    }

    public void AddEdgeType(EdgeType type, uint value)
    {
        _edgeTypeFrequency[(int)type] =
            Utilities.ThreadsafeAdd(
                ref _edgeTypeFrequency[(int)type],
                value);
    }

    public static string GetHeader()
    {
        return string.Join(_delimiter, new string[]
        {
            "BlockHeight",
            "Runtime(seconds)",
            "Confirmations",
            "Bits",
            "Difficulty",
            "Size",
            "StrippedSize",
            "Weight",
            "Retries",
            "BlockTxCount",
            "BlockTxInputsCount",
            "BlockTxOutputsCount",
            string.Join(
                _delimiter,
                ((EdgeType[])Enum.GetValues(typeof(EdgeType))).Select(
                    x => "BlockGraph" + x + "TxCount").ToArray()),
            string.Join(
                _delimiter,
                ((EdgeType[])Enum.GetValues(typeof(EdgeType))).Select(
                    x => "BlockGraph" + x + "TxSum").ToArray()),
        });
    }

    public override string ToString()
    {
        return string.Join(_delimiter, new string[]
        {
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
            InputTxCount.ToString(),
            OutputTxCount.ToString(),
            string.Join(
                _delimiter,
                _edgeTypeFrequency.Select((v, i) => v.ToString()).ToArray()),
            string.Join(
                _delimiter,
                _edgeTypeTxSum.Select((v, i) => v.ToString()).ToArray()),
            Environment.NewLine
        });
    }
}
