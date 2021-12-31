namespace BC2G.Model
{
    public class Edge
    {
        public string Source { get; }
        public string Target { get; }
        public double Value { get; }
        public EdgeType Type { get; }

        public Edge(
            string source, string target,
            double value, EdgeType type)
        {
            Source = source;
            Target = target;
            Value = value;
            Type = type;
        }

        public int GetHashCode(bool ignoreValue)
        {
            if (ignoreValue)
                return HashCode.Combine(Source, Target, Type);
            else
                return GetHashCode();
        }

        public override int GetHashCode()
        {
            // implemented based on https://stackoverflow.com/a/263416/947889
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 29 + Source.GetHashCode();
                hash = hash * 29 + Target.GetHashCode();
                hash = hash * 29 + Value.GetHashCode();
                hash = hash * 29 + Type.GetHashCode();
                return hash;
            }
        }
    }
}
