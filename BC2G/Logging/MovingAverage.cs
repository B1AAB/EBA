namespace BC2G.Logging
{
    public class MovingAverage
    {
        private double _average = 1;
        public double Average { get { return _average; } }

        /// <summary>
        /// Blocks per second.
        /// </summary>
        public double Speed { get { return Math.Round(1.0 / _average, 2); } }

        private readonly int _windowSize;
        private readonly Queue<double> _queue = new();
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
                    _queue.Dequeue();
                _queue.Enqueue(runtime);
                _average = Math.Round(_queue.Average(), 2);
            }
        }
    }
}
