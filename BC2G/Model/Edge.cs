namespace BC2G.Model
{
    public class Edge
    {
        public string Source { get; }
        public string Target { get; }
        public double Value { get; }
        public EdgeType Type { get; }
        public uint Timestamp { get; }


        public Edge(
            string source, string target,
            double value, EdgeType type,
            uint timestamp)
        {
            Source = source;
            Target = target;
            Value = value;
            Type = type;
            Timestamp = timestamp;
        }

        public int GetHashCode(bool ignoreValue)
        {
            if (ignoreValue)
                return HashCode.Combine(Source, Target, Type, Timestamp);
            else
                return GetHashCode();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Target, Value, Type, Timestamp);
        }
    }
}
