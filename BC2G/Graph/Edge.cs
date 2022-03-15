namespace BC2G.Graph
{
    public class Edge
    {
        public string Source { get; }
        public string Target { get; }
        public double Value { get; }
        public EdgeType Type { get; }
        public uint Timestamp { get; }
        public int BlockHeight { get; }

        public static string Header
        {
            get
            {
                return string.Join(_delimiter, new string[]
                {
                    "Source",
                    "Target",
                    "Weight", // TODO: rename this
                    "EdgeType",
                    "Timestamp",
                    "TimeOffsetFromGenesisBlock",
                    "BlockHeight"
                });
            }
        }

        private const string _delimiter = ",";


        public Edge(
            string source, string target,
            double value, EdgeType type,
            uint timestamp, int blockHeight)
        {
            Source = source;
            Target = target;
            Value = value;
            Type = type;
            Timestamp = timestamp;
            BlockHeight = blockHeight;
        }

        public string ToString(string sourceId, string targetId)
        {
            return string.Join(_delimiter, new string[]
            {
                sourceId,
                targetId,
                Value.ToString(),
                ((int)Type).ToString(),
                Timestamp.ToString(),
                (Timestamp - BitcoinAgent.GenesisTimestamp).ToString(),
                BlockHeight.ToString()
            });
        }

        public static Edge FromString(string[] fields)
        {
            return new Edge(
                fields[0],
                fields[1],
                double.Parse(fields[2]),
                Enum.Parse<EdgeType>(fields[3]),
                uint.Parse(fields[4]),
                int.Parse(fields[6]));
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
