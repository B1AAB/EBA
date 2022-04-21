using System.Diagnostics;

namespace BC2G.Graph
{
    public class GraphStatistics
    {
        public int Height { get; }
        public TimeSpan Runtime { get { return _runtime; } }
        private TimeSpan _runtime;
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

        private readonly double[] _edgeTypeTxSum = 
            new double[Enum.GetNames(typeof(EdgeType)).Length];

        private const char _delimiter = '\t';

        public GraphStatistics(int height)
        {
            Height = height;
        }

        public void StartStopwatch()
        {
            _stopwatch.Start();
        }
        public void StopStopwatch()
        {
            _stopwatch.Stop();
            _runtime = _stopwatch.Elapsed;
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
                "Runtime",
                "InputTxCount",
                "OutputTxCount",
                string.Join(
                    _delimiter,
                    ((EdgeType[])Enum.GetValues(typeof(EdgeType))).Select(
                        x => x + "TxCount").ToArray()),
                string.Join(
                    _delimiter,
                    ((EdgeType[])Enum.GetValues(typeof(EdgeType))).Select(
                        x => x + "TxSum").ToArray()),
            });
        }

        public override string ToString()
        {
            return string.Join(_delimiter, new string[]
            {
                Height.ToString(),
                Runtime.ToString(),
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
}
