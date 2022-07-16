using BC2G.Model;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;


// TODO: 
// instead of creating one a new node instance for the Coinbase node, 
// create one node instance and re-use it.
// 
//
// TODO:
// remove the RewardAddress from the BaseGraph.


namespace BC2G.Graph
{
    public class BlockGraph : GraphBase, IEquatable<BlockGraph>
    {
        public int Height { get; }
        public uint Timestamp { get; }
        public Block Block { get; }
        public BlockStatistics Stats { set; get; }

        /// <summary>
        /// Is the sum of all the tranactions fee.
        /// </summary>
        public double TotalFee { get { return _totalFee; } }
        private double _totalFee;

        public ReadOnlyCollection<Edge> Edges
        {
            get
            {
                return new ReadOnlyCollection<Edge>(_edges.Values.ToList());
            }
        }
        private readonly ConcurrentDictionary<int, Edge> _edges = new();

        public ReadOnlyCollection<Node> Nodes
        {
            get
            {
                return new ReadOnlyCollection<Node>(_nodes.Keys.ToList());
            }
        }
        private readonly ConcurrentDictionary<Node, byte> _nodes = new();

        public int NodeCount { get { return _nodes.Count; } }
        public int EdgeCount { get { return _edges.Count; } }

        private readonly ConcurrentQueue<TransactionGraph> _txGraphsQueue = new();

        public BlockGraph(Block block) : base()
        {
            Block = block;

            // TODO: with the above block ref, no need to keep a copy of height and timestamp.
            Height = block.Height;

            // See the following BIP on using `mediantime` instead of `time`.
            // https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
            Timestamp = block.MedianTime;

            Stats = new BlockStatistics(block);
            Stats.StartStopwatch();
        }

        // TODO: this constructor is required by the deserializer
        // mostly for testing purposes, should improve avoid
        // needing constructor.
        public BlockGraph(int height)
        {
            Height = height;
            Stats = new BlockStatistics(height);
            Stats.StartStopwatch();
        }

        public void Enqueue(TransactionGraph g)
        {
            Utilities.ThreadsafeAdd(ref _totalFee, g.Fee);
            _txGraphsQueue.Enqueue(g);
        }

        public void MergeQueuedTxGraphs(CancellationToken ct)
        {
            var coinbaseTxG = _txGraphsQueue.First(x => x.Sources.IsEmpty);
            double totalPaidToMiner = coinbaseTxG.Targets.Sum(x => x.Value);
            double blockReward = totalPaidToMiner - TotalFee;

            // First process all non-coinbase transactions;
            // this helps determine all the fee paied to the
            // miner in the block. In the Bitcoin chain, fee
            // is registered as a transfer from coinbase to 
            // miner. But here we process it as a transfer 
            // from sender to miner. 
            Parallel.ForEach(
                _txGraphsQueue.Where(x => !x.Sources.IsEmpty),
                (txGraph, state) =>
                {
                    if (ct.IsCancellationRequested)
                    { state.Stop(); return; }

                    Merge(txGraph, coinbaseTxG, totalPaidToMiner, ct);

                    if (ct.IsCancellationRequested)
                    { state.Stop(); return; }
                });

            foreach (var item in coinbaseTxG.Targets)
            {
                AddEdge(new Edge(
                    new Node(),
                    item.Key,
                    Utilities.Round((item.Value * blockReward) / totalPaidToMiner),
                    EdgeType.Generation,
                    Timestamp,
                    Height));
            }
        }

        private void Merge(
            TransactionGraph txGraph, 
            TransactionGraph coinbaseTxG, 
            double totalPaidToMiner, 
            CancellationToken ct)
        {
            var fee = txGraph.Fee;
            if (fee > 0.0)
            {
                // You cannot modify a collection that you're iterating over;
                // therefore, you need to iterate over a copy of the keys of 
                // the dictionary. There are different ways of implementing it, 
                // but probably the following requires least accesses to the
                // collection. 
                foreach (var s in txGraph.Sources)
                    txGraph.Sources.AddOrUpdate(
                        s.Key, s.Value,
                        (_, oldValue) => Utilities.Round(
                            oldValue - Utilities.Round(
                                oldValue * Utilities.Round(
                                    fee / txGraph.TotalInputValue))));
            }

            var sumInputWithoutFee = txGraph.TotalInputValue - fee;

            foreach (var s in txGraph.Sources)
            {
                if (ct.IsCancellationRequested)
                    return;

                var d = txGraph.TotalInputValue - fee;
                foreach (var t in txGraph.Targets)
                {
                    // It means the transaction is a "change" transfer 
                    // (i.e., return the remainder of a transfer to target
                    // to self), we avoid these transactions to simplify 
                    // graph representation. 
                    if (s.Key == t.Key)
                        continue;

                    var v = 0.0;
                    if (d != 0)
                        v = Utilities.Round(t.Value * Utilities.Round(s.Value / d));

                    AddEdge(new Edge(
                        s.Key, t.Key, v,
                        EdgeType.Transfer,
                        Timestamp,
                        Height));
                }

                var x = fee * Utilities.Round(s.Value / sumInputWithoutFee == 0 ? 1 : sumInputWithoutFee);
                if (fee > 0)
                    foreach (var m in coinbaseTxG.Targets)
                        AddEdge(new Edge(s.Key, m.Key,
                            Utilities.Round(x * Utilities.Round(m.Value / totalPaidToMiner)),
                            EdgeType.Fee, Timestamp, Height));
            }
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
                    edge.Timestamp,
                    edge.BlockHeight));

            if(double.IsNaN(edge.Value))
            {

            }

            _nodes.TryAdd(edge.Source, 0);
            _nodes.TryAdd(edge.Target, 0);
            Stats.IncrementEdgeType(edge.Type, edge.Value);
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
