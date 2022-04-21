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
                    "Value",
                    "EdgeType",
                    "TimeOffsetFromGenesisBlock",
                    "BlockHeight"
                });
            }
        }

        private const string _delimiter = "\t";


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
                (Timestamp - BitcoinAgent.GenesisTimestamp).ToString(),
                BlockHeight.ToString()
            });
        }

        public static Edge FromString(string[] fields)
        {
            return new Edge(
                source: fields[0],
                target: fields[1],
                value: double.Parse(fields[2]),
                type: Enum.Parse<EdgeType>(fields[3]),
                timestamp: BitcoinAgent.GenesisTimestamp + uint.Parse(fields[4]),
                blockHeight: int.Parse(fields[5]));
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
