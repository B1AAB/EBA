namespace BC2G.Model
{
    public class Edge
    {
        public string Source { get; private set; }
        public string Target { get; private set; }
        public double Value { get; private set; }
        public EdgeType Type { get; private set; }

        public Edge(
            string source, string target,
            double value, EdgeType type)
        {
            Source = source;
            Target = target;
            Value = value;
            Type = type;
        }
    }
}
