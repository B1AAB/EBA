using System.Collections.Concurrent;

namespace BC2G.Graph
{
    public class GraphBase : IEquatable<GraphBase>
    {
        public const string CoinbaseTxLabel = "Coinbase";

        protected readonly ConcurrentDictionary<string, double> _sources = new();
        protected readonly ConcurrentDictionary<string, double> _targets = new();

        public List<string> RewardsAddresses = new();

        public GraphBase()
        { }

        public bool Equals(GraphBase? other)
        {   
            if (other == null)
                return false;

            return ReferenceEquals(this, other);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as GraphBase);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
