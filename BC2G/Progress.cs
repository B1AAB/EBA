using BC2G.Logging;

namespace BC2G
{
    public class Progress
    {
        private readonly int _from;
        private readonly int _to;
        private readonly int _blocksCount;
        private int _processed;

        private const int _movinAvgWindow = 10;
        private readonly MovingAverage _runtimeMovingAverage;

        public Progress(int from, int to)
        {
            _from = from;
            _to = to;
            _blocksCount = _to - _from;
            _runtimeMovingAverage = new MovingAverage(_movinAvgWindow);
        }

        public void RecordProcessed(int txCount, double runtime)
        {
            _runtimeMovingAverage.Add(runtime);
            _processed = Interlocked.Increment(ref _processed);
        }

        public void IncrementProcessed()
        {
            _processed = Interlocked.Increment(ref _processed);
        }

        public override string ToString()
        {
            return $"Processed {_processed:n0} / {_blocksCount:n0}\t";
        }
    }
}
