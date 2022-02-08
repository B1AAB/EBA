using System.Collections.Concurrent;

namespace BC2G.Logging
{
    public class MovingAverage
    {
        private double _average = 1;
        public double Average { get { return _average; } }
        public double Speed
        {
            get { return Math.Round(1.0 / _average, 3); }
        }

        private readonly int _windowSize;
        private readonly ConcurrentQueue<double> _queue = new();
        private static readonly object _locker = new();

        public MovingAverage(int windowSize)
        {
            _windowSize = windowSize;
        }

        public void Add(double runtime)
        {
            lock (_locker)
            {
                if (_queue.Count == _windowSize)
                    _queue.TryDequeue(out double _);
                _queue.Enqueue(runtime);
                _average = _queue.Average();
            }
        }
    }
}
