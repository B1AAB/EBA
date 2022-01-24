using BC2G.Model;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace BC2G.Graph
{
    /// <summary>
    /// This class can be used to convert all the 
    /// sources (vin) and tagets (vout) of all transactions
    /// in a block to a graph. 
    /// 
    /// In order to use it: on each transaction (tx), use 
    /// the `AddSource` and `AddTarget` methods to add 
    /// sources (vin) and targets (vout), respectively.
    /// Then, **before moving to the next transaction**, 
    /// call the `UpdateGraph` method. If transaction is 
    /// not `coinbase`, you'll need to pass a list of 
    /// all the miner addresses to which the reward 
    /// is paid. This list will be used to collect 
    /// fees. 
    /// </summary>
    public class GraphBase2 : IEquatable<GraphBase>
    {
        public const string CoinbaseTxLabel = "Coinbase";

        public BlockStatistics Stats { get; } = new(0);

        public ReadOnlyCollection<Edge> Edges
        {
            get
            {
                return new ReadOnlyCollection<Edge>(_edges.Values.ToList());
            }
        }
        public ReadOnlyCollection<string> Nodes
        {
            get
            {
                return new ReadOnlyCollection<string>(_nodes.Keys.ToList());
            }
        }

        private readonly ConcurrentDictionary<int, Edge> _edges = new();
        private readonly ConcurrentDictionary<string, byte> _nodes = new();

        protected readonly ConcurrentDictionary<string, double> _sources = new();
        protected readonly ConcurrentDictionary<string, double> _targets = new();
        private double _totalInputValue;
        private double _totalOutputValue;

        public uint Timestamp { get; set; }

        public List<string> RewardsAddresses = new();

        public GraphBase2(BlockStatistics stats) : this()
        {
            Stats = stats;
        }
        public GraphBase2() { }

        public void Merge(TransactionGraph txGraph)
        {
            if (txGraph.sources.IsEmpty)
            {
                // build generative graph
                foreach (var item in txGraph.targets)
                    AddEdge(new Edge(
                        CoinbaseTxLabel,
                        item.Key,
                        item.Value,
                        EdgeType.Generation,
                        Timestamp));
            }
            else
            {
                double fee = Utilities.Round(txGraph.TotalInputValue - txGraph.TotalOutputValue);
                if (fee > 0.0)
                    foreach (var s in txGraph.sources)
                        txGraph.sources.AddOrUpdate(
                            s.Key, txGraph.sources[s.Key],
                            (_, oldValue) => Utilities.Round(
                                oldValue - Utilities.Round(
                                    oldValue * Utilities.Round(
                                        fee / txGraph.TotalInputValue))));
                /// The AddOrUpdate method is only expected to update, 
                /// adding a new key is not expected to happen. 

                foreach (var s in txGraph.sources)
                {
                    foreach (var t in txGraph.targets)
                        AddEdge(new Edge(
                            s.Key, t.Key,
                            Utilities.Round(t.Value * Utilities.Round(
                                s.Value / txGraph.TotalInputValue)),
                            s.Key == t.Key ? EdgeType.Change : EdgeType.Transfer,
                            Timestamp));

                    foreach (var m in RewardsAddresses)
                    {
                        var feeShare = Utilities.Round(fee / RewardsAddresses.Count);
                        if (feeShare > 0.0)
                            AddEdge(new Edge(s.Key, m, feeShare, EdgeType.Fee, Timestamp));
                    }
                }
            }
        }

        public void UpdateGraph(uint timestamp, List<string>? rewardAddresses = null)
        {
            if (_sources.IsEmpty)
                BuildGenerativeTxGraph(timestamp);
            else
            {
                if (rewardAddresses == null)
                    throw new ArgumentNullException(
                        nameof(rewardAddresses),
                        $"{nameof(rewardAddresses)} cannot be null");
                else
                    BuildTxGraph(rewardAddresses, timestamp);
            }

            _sources.Clear();
            _targets.Clear();
            _totalInputValue = 0;
            _totalOutputValue = 0;
        }

        public void AddEdge(Edge edge)
        {
            /// Note that the hashkey is invariant to the edge value.
            /// If this is changed, the `Equals` method needs to be
            /// updated accordingly.
            _edges.AddOrUpdate(
                edge.GetHashCode(true), edge,
                (key, oldValue) => new Edge(
                    edge.Source,
                    edge.Target,
                    edge.Value + oldValue.Value,
                    edge.Type,
                    edge.Timestamp));

            _nodes.TryAdd(edge.Source, 0);
            _nodes.TryAdd(edge.Target, 0);
        }

        public void AddEdges(ICollection<Edge> edges)
        {
            foreach (var edge in edges)
                AddEdge(edge);
        }

        public string AddSource(string source, double value)
        {
            _totalInputValue = Utilities.ThreadsafeAdd(ref _totalInputValue, value);
            return AddInOrOut(_sources, source, value);
        }

        public string AddTarget(string target, double value)
        {
            _totalOutputValue = Utilities.ThreadsafeAdd(ref _totalOutputValue, value);
            return AddInOrOut(_targets, target, value);
        }

        private static string AddInOrOut(
            ConcurrentDictionary<string, double> collection,
            string address,
            double value)
        {
            collection.AddOrUpdate(
                address, Utilities.Round(value),
                (_, oldValue) => Utilities.Round(oldValue + value));

            return address;
        }

        private void BuildGenerativeTxGraph(uint timestamp)
        {
            foreach (var item in _targets)
                AddEdge(new Edge(
                    CoinbaseTxLabel,
                    item.Key,
                    item.Value,
                    EdgeType.Generation,
                    timestamp));
        }

        private void BuildTxGraph(List<string> rewardAddresses, uint timestamp)
        {
            double fee = Utilities.Round(_totalInputValue - _totalOutputValue);
            if (fee > 0.0)
                foreach (var s in _sources)
                    _sources.AddOrUpdate(
                        s.Key, _sources[s.Key],
                        (_, oldValue) => Utilities.Round(
                            oldValue - Utilities.Round(
                                oldValue * Utilities.Round(
                                    fee / _totalInputValue))));
            /// The AddOrUpdate method is only expected to update, 
            /// adding a new key is not expected to happen. See
            /// the above comment.

            foreach (var s in _sources)
            {
                foreach (var t in _targets)
                    AddEdge(new Edge(
                        s.Key, t.Key,
                        Utilities.Round(t.Value * Utilities.Round(
                            s.Value / _totalInputValue)),
                        s.Key == t.Key ? EdgeType.Change : EdgeType.Transfer,
                        timestamp));

                foreach (var m in rewardAddresses)
                {
                    var feeShare = Utilities.Round(fee / rewardAddresses.Count);
                    if (feeShare > 0.0)
                        AddEdge(new Edge(s.Key, m, feeShare, EdgeType.Fee, timestamp));
                }
            }
        }



        public bool Equals(GraphBase? other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            var otherNodes = other.Nodes;
            if (_nodes.Count != otherNodes.Count)
                return false;

            var otherEdges = other.Edges;
            if (_edges.Count != otherEdges.Count)
                return false;

            var equal = Enumerable.SequenceEqual(
                Nodes.OrderBy(x => x),
                otherNodes.OrderBy(x => x));

            if (!equal)
                return false;

            var hashes = new HashSet<int>(_edges.Keys);
            foreach (var edge in otherEdges)
                /// Note that this hash method does not include
                /// edge value in the computation of hash key;
                /// this is in accordance with home with _edges.Keys
                /// are generated in the AddEdge method.
                if (!hashes.Remove(edge.GetHashCode(true)))
                    return false;

            if (hashes.Count > 0)
                return false;

            return true;
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






    public class GraphBase : IEquatable<GraphBase>
    {
        public const string CoinbaseTxLabel = "Coinbase";

        public BlockStatistics Stats { get; } = new(0);

        public ReadOnlyCollection<Edge> Edges
        {
            get
            {
                return new ReadOnlyCollection<Edge>(_edges.Values.ToList());
            }
        }
        public ReadOnlyCollection<string> Nodes
        {
            get
            {
                return new ReadOnlyCollection<string>(_nodes.Keys.ToList());
            }
        }

        private readonly ConcurrentDictionary<int, Edge> _edges = new();
        private readonly ConcurrentDictionary<string, byte> _nodes = new();

        protected readonly ConcurrentDictionary<string, double> _sources = new();
        protected readonly ConcurrentDictionary<string, double> _targets = new();
        private double _totalInputValue;
        private double _totalOutputValue;

        public uint Timestamp { get; set; }

        public List<string> RewardsAddresses = new();

        public GraphBase(BlockStatistics stats) : this()
        {
            Stats = stats;
        }
        public GraphBase() { }

        public void Merge(TransactionGraph txGraph, CancellationToken cancellationToken)
        {
            if (txGraph.sources.IsEmpty)
            {
                // build generative graph
                foreach (var item in txGraph.targets)
                    AddEdge(new Edge(
                        CoinbaseTxLabel,
                        item.Key,
                        item.Value,
                        EdgeType.Generation,
                        Timestamp));
            }
            else
            {
                double fee = Utilities.Round(txGraph.TotalInputValue - txGraph.TotalOutputValue);
                if (fee > 0.0)
                    foreach (var s in txGraph.sources)
                        txGraph.sources.AddOrUpdate(
                            s.Key, txGraph.sources[s.Key],
                            (_, oldValue) => Utilities.Round(
                                oldValue - Utilities.Round(
                                    oldValue * Utilities.Round(
                                        fee / txGraph.TotalInputValue))));
                /// The AddOrUpdate method is only expected to update, 
                /// adding a new key is not expected to happen.

                foreach (var s in txGraph.sources)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    foreach (var t in txGraph.targets)
                        AddEdge(new Edge(
                            s.Key, t.Key,
                            Utilities.Round(t.Value * Utilities.Round(
                                s.Value / txGraph.TotalInputValue)),
                            s.Key == t.Key ? EdgeType.Change : EdgeType.Transfer,
                            Timestamp));

                    foreach (var m in RewardsAddresses)
                    {
                        var feeShare = Utilities.Round(fee / RewardsAddresses.Count);
                        if (feeShare > 0.0)
                            AddEdge(new Edge(s.Key, m, feeShare, EdgeType.Fee, Timestamp));
                    }
                }
            }
        }

        public void UpdateGraph(uint timestamp, List<string>? rewardAddresses = null)
        {
            if (_sources.IsEmpty)
                BuildGenerativeTxGraph(timestamp);
            else
            {
                if (rewardAddresses == null)
                    throw new ArgumentNullException(
                        nameof(rewardAddresses),
                        $"{nameof(rewardAddresses)} cannot be null");
                else
                    BuildTxGraph(rewardAddresses, timestamp);
            }

            _sources.Clear();
            _targets.Clear();
            _totalInputValue = 0;
            _totalOutputValue = 0;
        }

        public void AddEdge(Edge edge)
        {
            /// Note that the hashkey is invariant to the edge value.
            /// If this is changed, the `Equals` method needs to be
            /// updated accordingly.
            _edges.AddOrUpdate(
                edge.GetHashCode(true), edge,
                (key, oldValue) => new Edge(
                    edge.Source,
                    edge.Target,
                    edge.Value + oldValue.Value,
                    edge.Type,
                    edge.Timestamp));

            _nodes.TryAdd(edge.Source, 0);
            _nodes.TryAdd(edge.Target, 0);
        }

        public void AddEdges(ICollection<Edge> edges)
        {
            foreach (var edge in edges)
                AddEdge(edge);
        }

        public string AddSource(string source, double value)
        {
            _totalInputValue = Utilities.ThreadsafeAdd(ref _totalInputValue, value);
            return AddInOrOut(_sources, source, value);
        }

        public string AddTarget(string target, double value)
        {
            _totalOutputValue = Utilities.ThreadsafeAdd(ref _totalOutputValue, value);
            return AddInOrOut(_targets, target, value);
        }

        private static string AddInOrOut(
            ConcurrentDictionary<string, double> collection,
            string address,
            double value)
        {
            collection.AddOrUpdate(
                address, Utilities.Round(value),
                (_, oldValue) => Utilities.Round(oldValue + value));

            return address;
        }

        private void BuildGenerativeTxGraph(uint timestamp)
        {
            foreach (var item in _targets)
                AddEdge(new Edge(
                    CoinbaseTxLabel,
                    item.Key,
                    item.Value,
                    EdgeType.Generation,
                    timestamp));
        }

        private void BuildTxGraph(List<string> rewardAddresses, uint timestamp)
        {
            double fee = Utilities.Round(_totalInputValue - _totalOutputValue);
            if (fee > 0.0)
                foreach (var s in _sources)
                    _sources.AddOrUpdate(
                        s.Key, _sources[s.Key],
                        (_, oldValue) => Utilities.Round(
                            oldValue - Utilities.Round(
                                oldValue * Utilities.Round(
                                    fee / _totalInputValue))));
            /// The AddOrUpdate method is only expected to update, 
            /// adding a new key is not expected to happen. See
            /// the above comment.

            foreach (var s in _sources)
            {
                foreach (var t in _targets)
                    AddEdge(new Edge(
                        s.Key, t.Key,
                        Utilities.Round(t.Value * Utilities.Round(
                            s.Value / _totalInputValue)),
                        s.Key == t.Key ? EdgeType.Change : EdgeType.Transfer,
                        timestamp));

                foreach (var m in rewardAddresses)
                {
                    var feeShare = Utilities.Round(fee / rewardAddresses.Count);
                    if (feeShare > 0.0)
                        AddEdge(new Edge(s.Key, m, feeShare, EdgeType.Fee, timestamp));
                }
            }
        }



        public bool Equals(GraphBase? other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            var otherNodes = other.Nodes;
            if (_nodes.Count != otherNodes.Count)
                return false;

            var otherEdges = other.Edges;
            if (_edges.Count != otherEdges.Count)
                return false;

            var equal = Enumerable.SequenceEqual(
                Nodes.OrderBy(x => x),
                otherNodes.OrderBy(x => x));

            if (!equal)
                return false;

            var hashes = new HashSet<int>(_edges.Keys);
            foreach (var edge in otherEdges)
                /// Note that this hash method does not include
                /// edge value in the computation of hash key;
                /// this is in accordance with home with _edges.Keys
                /// are generated in the AddEdge method.
                if (!hashes.Remove(edge.GetHashCode(true)))
                    return false;

            if (hashes.Count > 0)
                return false;

            return true;
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
