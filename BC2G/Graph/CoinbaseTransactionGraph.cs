using BC2G.Model;

namespace BC2G.Graph
{
    public class CoinbaseTransactionGraph : BaseGraph
    {
        private readonly string _coinbaseTxLabel;
        private readonly Dictionary<string, double> _targets;

        public CoinbaseTransactionGraph(string coinbaseTxLabel, int targetsCount)
        {
            _coinbaseTxLabel = coinbaseTxLabel;
            _targets = new Dictionary<string, double>(targetsCount);
        }

        public void AddTarget(string target, double value)
        {
            if (!_targets.ContainsKey(target))
                _targets.Add(target, 0);

            // FIXME.
            _targets[target] = Math.Round(_targets[target] + value, digits: 8);
        }

        public void UpdateEdges()
        {
            foreach (var item in _targets)
                AddEdge(new Edge(
                    _coinbaseTxLabel,
                    item.Key,
                    item.Value,
                    EdgeType.Generation));
        }
    }
}
