namespace BC2G.Graph
{
    public class GraphBase : IEquatable<GraphBase>
    {
        public const string CoinbaseTxLabel = "Coinbase";

        protected readonly ConcurrentDictionary<Node, double> _sources = new();
        protected readonly ConcurrentDictionary<Node, double> _targets = new();

        public List<Node> RewardsAddresses { set; get; } = new();

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
