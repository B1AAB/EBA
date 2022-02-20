using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace BC2G.Graph
{
    public class BlockGraph : GraphBase, IEquatable<BlockGraph>
    {
        public int Height { get; }
        public uint Timestamp { get; set; }
        public GraphStatistics Stats { set; get; }

        public ReadOnlyCollection<Edge> Edges
        {
            get
            {
                return new ReadOnlyCollection<Edge>(_edges.Values.ToList());
            }
        }
        private readonly ConcurrentDictionary<int, Edge> _edges = new();

        public ReadOnlyCollection<string> Nodes
        {
            get
            {
                return new ReadOnlyCollection<string>(_nodes.Keys.ToList());
            }
        }
        private readonly ConcurrentDictionary<string, byte> _nodes = new();

        public int NodeCount { get { return _nodes.Count; } }
        public int EdgeCount { get { return _edges.Count; } }

        private readonly ConcurrentQueue<TransactionGraph> _txGraphsQueue = new();

        public BlockGraph(int height):base()
        {
            Height = height;
            Stats = new GraphStatistics(height);
            Stats.StartStopwatch();
        }

        public void Enqueue(TransactionGraph g)
        {
            _txGraphsQueue.Enqueue(g);
        }

        public void MergeQueuedTxGraphs(CancellationToken ct)
        {
            Parallel.ForEach(_txGraphsQueue,
                (txGraph, state) =>
                {
                    if (ct.IsCancellationRequested)
                    { state.Stop(); return; }

                    Merge(txGraph, ct);

                    if (ct.IsCancellationRequested)
                    { state.Stop(); return; }
                });
        }

        private void Merge(TransactionGraph txGraph, CancellationToken ct)
        {
            if (txGraph.Sources.IsEmpty)
            {
                // build generative graph
                foreach (var item in txGraph.Targets)
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
                    foreach (var s in txGraph.Sources)
                        txGraph.Sources.AddOrUpdate(
                            s.Key, txGraph.Sources[s.Key],
                            (_, oldValue) => Utilities.Round(
                                oldValue - Utilities.Round(
                                    oldValue * Utilities.Round(
                                        fee / txGraph.TotalInputValue))));
                /// The AddOrUpdate method is only expected to update, 
                /// adding a new key is not expected to happen.

                foreach (var s in txGraph.Sources)
                {
                    if (ct.IsCancellationRequested)
                        return;

                    foreach (var t in txGraph.Targets)
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

        // TODO: it could probably be faster if this method takes the attributes of Edge instead of an instance of edge.
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
            Stats.IncrementEdgeType(edge.Type);
        }

        public bool Equals(BlockGraph? other)
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
            return Equals(obj as BlockGraph);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
