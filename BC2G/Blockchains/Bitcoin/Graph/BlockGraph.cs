namespace BC2G.Blockchains.Bitcoin.Graph;

public class BitcoinBlockGraph : GraphBase, IEquatable<BitcoinBlockGraph>
{
    public long Height { get; }
    public uint Timestamp { get; }
    public Block Block { get; }
    public BlockStatistics Stats { set; get; }

    public List<ScriptNode> RewardsAddresses { set; get; } = new();

    /// <summary>
    /// Is the sum of all the tranactions fee.
    /// </summary>
    public double TotalFee { get { return _totalFee; } }
    private double _totalFee;

    // TODO: can base types used for this purpose?!!
    /*public new ReadOnlyCollection<S2SEdge> Edges
    {
        get
        {
            return new ReadOnlyCollection<S2SEdge>(_edges.Values.ToList());
        }
    }*/
    //private readonly ConcurrentDictionary<int, S2SEdge> _edges = new();
    /*
    public new ReadOnlyCollection<ScriptNode> Nodes
    {
        get
        {
            return new ReadOnlyCollection<ScriptNode>(_nodes.Keys.ToList());
        }
    }*/
    //private readonly ConcurrentDictionary<ScriptNode, byte> _nodes = new();

    private readonly TransactionGraph _coinbaseTxGraph;

    private readonly ConcurrentQueue<TransactionGraph> _txGraphsQueue = new();

    public BitcoinBlockGraph(Block block, TransactionGraph coinbaseTxGraph) : base()
    {
        Block = block;
        _coinbaseTxGraph = coinbaseTxGraph;

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
    public BitcoinBlockGraph(long height)
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
        double totalPaidToMiner = _coinbaseTxGraph.TargetScripts.Sum(x => x.Value);
        double blockReward = totalPaidToMiner - TotalFee;

        AddOrUpdateEdge(new C2TEdge(_coinbaseTxGraph.TxNode, blockReward, Timestamp, Height));

        // First process all non-coinbase transactions;
        // this helps determine all the fee paied to the
        // miner in the block. In the Bitcoin chain, fee
        // is registered as a transfer from coinbase to 
        // miner. But here we process it as a transfer 
        // from sender to miner. 
        Parallel.ForEach(_txGraphsQueue,
            #if (DEBUG)
            parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = 1 },
            #endif
            (txGraph, state) =>
            {
                if (ct.IsCancellationRequested)
                { state.Stop(); return; }

                Merge(txGraph, _coinbaseTxGraph, totalPaidToMiner, ct);

                if (ct.IsCancellationRequested)
                { state.Stop(); return; }
            });

        foreach (var item in _coinbaseTxGraph.TargetScripts)
        {
            AddOrUpdateEdge(new C2SEdge(
                item.Key,
                Utilities.Round((item.Value * blockReward) / totalPaidToMiner),
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
            foreach (var s in txGraph.SourceScripts)
                txGraph.SourceScripts.AddOrUpdate(
                    s.Key, s.Value,
                    (_, oldValue) => Utilities.Round(
                        oldValue - Utilities.Round(
                            oldValue * Utilities.Round(
                                fee / txGraph.TotalInputValue))));

            AddOrUpdateEdge(new T2TEdge(txGraph.TxNode, coinbaseTxG.TxNode, fee, EdgeType.Fee, Timestamp, Height));
        }

        var sumInputWithoutFee = txGraph.TotalInputValue - fee;

        foreach (var s in txGraph.SourceScripts)
        {
            if (ct.IsCancellationRequested)
                return;

            var d = txGraph.TotalInputValue - fee;
            foreach (var t in txGraph.TargetScripts)
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

                AddOrUpdateEdge(new S2SEdge(
                    s.Key, t.Key, v,
                    EdgeType.Transfer,
                    Timestamp,
                    Height));
            }

            var x = fee * Utilities.Round(s.Value / sumInputWithoutFee == 0 ? 1 : sumInputWithoutFee);
            if (fee > 0)
                foreach (var m in coinbaseTxG.TargetScripts)
                    AddOrUpdateEdge(new S2SEdge(s.Key, m.Key,
                        Utilities.Round(x * Utilities.Round(m.Value / totalPaidToMiner)),
                        EdgeType.Fee, Timestamp, Height));
        }

        if (ct.IsCancellationRequested)
            return;

        foreach (var tx in txGraph.SourceTxes)
        {
            if (tx.Key == txGraph.TxNode.Id) // TODO: check when/if this can happen.
                continue;

            AddOrUpdateEdge(new T2TEdge(
                new TxNode(tx.Key), txGraph.TxNode, tx.Value, EdgeType.Transfer, Timestamp, Height));
        }
    }

    private void AddOrUpdateEdge(C2TEdge edge)
    {
        AddOrUpdateEdge(edge, (key, oldValue) => edge.Update(oldValue.Value));
        Stats.IncrementEdgeType(edge.Type, edge.Value);
    }

    public void AddOrUpdateEdge(T2TEdge edge)
    {
        AddOrUpdateEdge(edge, (_, oldEdge) => { return T2TEdge.Update((T2TEdge)oldEdge, edge); });
        Stats.IncrementEdgeType(edge.Type, edge.Value);
    }
    /*
    private void AddOrUpdateEdge(
        TxNode sourceTx, TxNode targetTx, double value, EdgeType type,
        uint? timestamp = null, long? height = null)
    {
        var e = new T2TEdge(sourceTx, targetTx, value, type, timestamp ?? Timestamp, height ?? Height);
        AddOrUpdateEdge(e, (_, oldEdge) => { return T2TEdge.Update((T2TEdge)oldEdge, e); });

        Stats.IncrementEdgeType(type, value);
    }*/

    public void AddOrUpdateEdge(S2SEdge edge)
    {
        /// Note that the hashkey is invariant to the edge value.
        /// If this is changed, the `Equals` method needs to be
        /// updated accordingly.
        /*_edges.AddOrUpdate(
            edge.GetHashCodeInt(true), edge,
            (key, oldValue) => new S2SEdge(
                edge.Source,
                edge.Target,
                edge.Value + oldValue.Value,
                edge.Type,
                edge.Timestamp,
                edge.BlockHeight));*/

        // TODO: ^^ this is not needed anymore.

        AddOrUpdateEdge(edge, (key, oldValue) => edge.Update(oldValue.Value));

        // TODO: are these needed?!!
        //_nodes.TryAdd(edge.Source, 0);
        //_nodes.TryAdd(edge.Target, 0);

        Stats.IncrementEdgeType(edge.Type, edge.Value);
    }

    public bool Equals(BitcoinBlockGraph? other)
    {
        if (other == null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        var otherNodes = other.Nodes;
        if (NodeCount != other.NodeCount)
        //if (_nodes.Count != otherNodes.Count)
            return false;

        //var otherEdges = other.Edges;
        //if (_edges.Count != otherEdges.Count)
        if (EdgeCount != other.EdgeCount)
            return false;

        var equal = Enumerable.SequenceEqual(
            Nodes.OrderBy(x => x),
            otherNodes.OrderBy(x => x));

        if (!equal)
            return false;

        throw new NotImplementedException();

       /* var hashes = new HashSet<int>(_edges.Keys);
        foreach (var edge in otherEdges)
            /// Note that this hash method does not include
            /// edge value in the computation of hash key;
            /// this is in accordance with home with _edges.Keys
            /// are generated in the AddEdge method.
            if (!hashes.Remove(edge.GetHashCodeInt(true)))
                return false;

        if (hashes.Count > 0)
            return false;

        return true;*/
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BitcoinBlockGraph);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
