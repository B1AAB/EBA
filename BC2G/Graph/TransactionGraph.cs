using BC2G.Model;

namespace BC2G.Graph
{
    public class AddressMetadata
    {
        public double Value { get; set; } = 0.0;
        public bool IsAddressKnown { get; set; } = true;
    }

    public class TransactionGraph : BaseGraph
    {
        // To be consistent with Bitcoin client. 
        public const int FractionalDigitsCount = 8;        

        private readonly Dictionary<string, AddressMetadata> _sources;
        private readonly Dictionary<string, double> _targets;
        private readonly List<string> _rewardsAddresses;

        private double _totalInputValue;
        private double _totalOutputValue;

        public TransactionGraph(int sourceSize, int tagetSize, List<string> rewardAddresses)
        {
            _sources = new Dictionary<string, AddressMetadata>(sourceSize);
            _targets = new Dictionary<string, double>(tagetSize);
            _rewardsAddresses = rewardAddresses;
        }

        public void AddSource(string source, double value)
        {
            if (string.IsNullOrEmpty(source))
            {
                do source = Utilities.GetRandomString(16);
                while (_sources.ContainsKey(source));
                _sources.Add(source,
                    new AddressMetadata()
                    {
                        IsAddressKnown = false
                    });
            }
            else if (!_sources.ContainsKey(source))
            {
                _sources.Add(source, new AddressMetadata());
            }

            _sources[source].Value = Round(_sources[source].Value + value);
            _totalInputValue = Round(_totalInputValue + value);
        }

        public void AddTarget(string target, double value)
        {
            if (!_targets.ContainsKey(target))
                _targets.Add(target, 0);

            _targets[target] = Round(_targets[target] + value);
            _totalOutputValue = Round(_totalOutputValue + value);
        }

        public void UpdateEdges()
        {
            double fee = Round(_totalInputValue - _totalOutputValue);

            var changes = _targets.Where(x => _sources.ContainsKey(x.Key)).ToList();
            switch (changes.Count)
            {
                case 0: break; // no change, all the input is sent to the output.
                case 1:
                    _sources[changes[0].Key].Value = Round(_sources[changes[0].Key].Value - changes[0].Value);
                    _targets.Remove(changes[0].Key);
                    _totalInputValue = Round(_totalInputValue - changes[0].Value);
                    break;
                default:
                    // This is not expected to happen.
                    throw new NotImplementedException();
            }

            foreach (var s in _sources)
                _sources[s.Key].Value = Round(_sources[s.Key].Value - fee);

            foreach (var s in _sources.Where(x => x.Value.IsAddressKnown))
            {
                foreach (var t in _targets)
                    AddEdge(new Edge(
                        s.Key, t.Key,
                        Round(t.Value * Round(s.Value.Value / Round(_totalInputValue - fee))),
                        EdgeType.Transfer));

                foreach (var m in _rewardsAddresses)
                    AddEdge(new Edge(
                        s.Key, m,
                        Round(fee / _rewardsAddresses.Count),
                        EdgeType.Fee));
            }
        }

        private double Round(double input)
        {
            // Regarding the motivation behind this, read the following:
            // https://stackoverflow.com/q/588004/947889
            return Math.Round(input, digits: FractionalDigitsCount);
        }
    }
}
