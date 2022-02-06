using System.Diagnostics;

namespace BC2G.Graph
{
    public class GraphStatistics
    {
        public int Height { get; }
        public TimeSpan Runtime { get { return _runtime; } }
        private TimeSpan _runtime;
        private readonly Stopwatch _stopwatch = new();

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

        public void IncrementEdgeType(EdgeType type)
        {
            Interlocked.Increment(ref _edgeTypeFrequency[(int)type]);
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
                string.Join(_delimiter, (EdgeType[]) Enum.GetValues(typeof(EdgeType)))
            });
        }

        public override string ToString()
        {
            return string.Join(_delimiter, new string[]
            {
                Height.ToString(),
                Runtime.ToString(),
                string.Join(
                    _delimiter,
                    _edgeTypeFrequency.Select((v, i) => v.ToString()).ToArray()),
                Environment.NewLine
            });
        }
    }
}
