using System.Collections.Concurrent;

namespace BC2G.Logging
{
    public abstract class BlockTraversalLoggingBase
    {
        public int MovingAvgWindowSize { set; get; } = 10;

        public int FromInclusive { get; }
        public int ToExclusive { get; }
        public int BlocksCount { get; }
        public double Total { get; }
        public double Percentage { get { return (Completed / Total) * 100; } }

        public int Completed { get { return _completed; } }
        private int _completed;

        public string ActiveBlocks
        {
            get
            {
                return
                    $"{_activeBlocks.Keys.Count,9}: " +
                    $"{string.Join(", ", _activeBlocks.Keys)}";
            }
        }
        private readonly ConcurrentDictionary<int, bool> _activeBlocks = new();
        public int ActiveBlocksCount { get { return _activeBlocks.Count; } }

        public MovingAverage BlockRuntimeMovingAvg { get; }

        private const string _cancelling = "Cancelling ... do not turn off your computer.";

        public BlockTraversalLoggingBase(
            int fromInclusive, 
            int toExclusive, 
            int blocksCount,
            int templateLinesCount)
        {
            FromInclusive = fromInclusive;
            ToExclusive = toExclusive;
            Total = blocksCount; //ToExclusive - FromInclusive;

            BlockRuntimeMovingAvg = new MovingAverage(MovingAvgWindowSize);

            // The exception is thrown with the message 'The handle is invalid.'
            // only when running the tests, because Xunit does not have a console.
            try { Console.CursorVisible = false; }
            catch (IOException) { }

            AsyncConsole.BookmarkCurrentLine();
            AsyncConsole.WriteLine("");
            for (int i = 0; i <= templateLinesCount; i++)
                AsyncConsole.WriteLine("");
        }

        public virtual string Log(int height)
        {
            _activeBlocks.TryAdd(height, true);
            ToConsole();
            return $"Started processing block {height}.";
        }

        public virtual string Log(
            int height,
            double runtime)
        {
            _activeBlocks.TryRemove(height, out bool _);

            Interlocked.Increment(ref _completed);

            ToConsole();

            return
                $"Active:{ActiveBlocksCount};" +
                $"Completed:{Completed}/{Total};" +
                $"{BlockRuntimeMovingAvg.Speed}bps.";
        }

        protected abstract void ToConsole();

        public virtual string LogCancelling()
        {
            AsyncConsole.WriteLine(_cancelling, ConsoleColor.Yellow);
            return _cancelling;
        }
    }
}
