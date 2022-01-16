namespace BC2G.Model
{
    public class BlockStatistics
    {
        private const char _delimiter = '\t';
        public int Height { get; }

        private bool _isRuntimeSet = false;
        private TimeSpan _runtime;
        public TimeSpan Runtime
        {
            // This property is writable only once.
            set
            {
                if (_isRuntimeSet)
                    throw new InvalidOperationException("Runtime is already set.");
                _runtime = value;
                _isRuntimeSet = true;
            }
            get
            {
                if (!_isRuntimeSet)
                    throw new InvalidOperationException("Runtime is not set yet.");
                return _runtime;
            }
        }

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

        public BlockStatistics(int height)
        {
            Height = height;
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

        public override string ToString()
        {
            return string.Join(_delimiter, new string[]
            {
                Height.ToString(),
                Runtime.ToString(),
                string.Join(
                    _delimiter,
                    _edgeTypeFrequency.Select((v, i) => v.ToString()).ToArray())
            });
        }
    }
}
