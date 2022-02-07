using System.Collections.Concurrent;

namespace BC2G.Logging
{
    public class ConsoleLoggingBase
    {
        public int MovingAvgWindowSize { set; get; } = 20;

        public int FromInclusive { get; }
        public int ToExclusive { get; }
        public double Total { get; }
        public double Percentage { get { return (Completed / Total) * 100; } }

        public int Completed { get { return _completed; } }
        private int _completed;

        public int NodesCount { get { return _nodesCount; } }
        private int _nodesCount;

        public int EdgesCount { get { return _edgesCount; } }
        private int _edgesCount;

        public string ActiveBlocks
        {
            get
            {
                return
                    $"{_activeBlocks.Keys.Count,10}: " +
                    $"{string.Join(", ", _activeBlocks.Keys)}";
            }
        }

        public MovingAverage BlockRuntimeMovingAvg { get; }
        public MovingAverage EdgeRuntimeMovingAvg { get; }

        private const string _cancelling = "Cancelling ... do not turn off your computer.";

        private readonly ConcurrentDictionary<int, bool> _activeBlocks = new();

        public ConsoleLoggingBase(int fromInclusive, int toExclusive, int templateLinesCount)
        {
            FromInclusive = fromInclusive;
            ToExclusive = toExclusive;
            Total = ToExclusive - FromInclusive;

            BlockRuntimeMovingAvg = new MovingAverage(MovingAvgWindowSize);
            EdgeRuntimeMovingAvg = new MovingAverage(MovingAvgWindowSize);

            AsyncConsole.BookmarkCurrentLine();
            AsyncConsole.WriteLine("");
            for (int i = 0; i <= templateLinesCount; i++)
                AsyncConsole.WriteLine("");
        }

        public virtual void Log(int height)
        {
            _activeBlocks.TryAdd(height, true);
        }

        public virtual void Log(
            int height, 
            int allNodesCount, 
            int addedEdgesCount, 
            double runtime)
        {
            _activeBlocks.TryRemove(height, out bool _);

            Interlocked.Increment(ref _completed);
            Interlocked.Add(ref _edgesCount, addedEdgesCount);
            _nodesCount = allNodesCount;

            BlockRuntimeMovingAvg.Add(runtime);
            EdgeRuntimeMovingAvg.Add(runtime / addedEdgesCount);
        }

        public virtual string LogCancelling()
        {
            AsyncConsole.WriteLine(_cancelling, ConsoleColor.Yellow);
            return _cancelling;
        }
    }
}
