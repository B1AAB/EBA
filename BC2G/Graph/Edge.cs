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
                    "Weight",
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
