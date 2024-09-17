using BC2G.Utilities;

namespace BC2G.Blockchains.Bitcoin.Graph;

public class BlockGraph : BitcoinGraph, IEquatable<BlockGraph>
{
    public long Height { get; }
    public uint Timestamp { get; }
    public Block Block { get; }
    public BlockStatistics Stats { set; get; }

    public List<ScriptNode> RewardsAddresses { set; get; } = [];

    /// <summary>
    /// Is the sum of all the tranactions fee.
    /// </summary>
    public double TotalFee { get { return _totalFee; } }
    private double _totalFee;

    private readonly TransactionGraph _coinbaseTxGraph;

    private readonly ConcurrentQueue<TransactionGraph> _txGraphsQueue = new();

    private readonly ILogger<BitcoinAgent> _logger;

    public BlockGraph(Block block, TransactionGraph coinbaseTxGraph, ILogger<BitcoinAgent> logger) : base()
    {
        Block = block;
        _coinbaseTxGraph = coinbaseTxGraph;
        _logger = logger;

        // TODO: with the above block ref, no need to keep a copy of height and timestamp.
        Height = block.Height;

        // See the following BIP on using `mediantime` instead of `time`.
        // https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
        Timestamp = block.MedianTime;

        Stats = new BlockStatistics(block);
        Stats.StartStopwatch();
    }

    public void Enqueue(TransactionGraph g)
    {
        Helpers.ThreadsafeAdd(ref _totalFee, g.Fee);
        _txGraphsQueue.Enqueue(g);
    }

    public void MergeQueuedTxGraphs(CancellationToken ct)
    {
        double miningReward = _coinbaseTxGraph.TargetScripts.Sum(x => x.Value);
        double mintedBitcoins = miningReward - TotalFee;

        Stats.MintedBitcoins = mintedBitcoins;
        Stats.TxFees = TotalFee;

        AddOrUpdateEdge(new C2TEdge(_coinbaseTxGraph.TxNode, mintedBitcoins, Timestamp, Height));

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

                Merge(txGraph, _coinbaseTxGraph, miningReward, ct);

                if (ct.IsCancellationRequested)
                { state.Stop(); return; }
            });

        foreach (var item in _coinbaseTxGraph.TargetScripts)
        {
            AddOrUpdateEdge(new C2SEdge(
                item.Key,
                Helpers.Round((item.Value * mintedBitcoins) / miningReward),
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
        // VERY IMPORTANT TODO: THIS IS TEMPORARY UNTIL A GOOD SOLUTION IS IMPLEMENTED.
        if (txGraph.SourceScripts.Count > 20 && txGraph.TargetScripts.Count > 20)
        {
            _logger.LogWarning(
                "Skipping a transaction because it contains more than 20 source and target nodes, " +
                "maximum currently supported. " +
                "Block: {b:n0}; " +
                "source scripts count: {s:n0}; " +
                "target scripts count: {t:n0}; " +
                "transaction hash: {tx}.",
                Height,
                txGraph.SourceScripts.Count,
                txGraph.TargetScripts.Count,
                txGraph.TxNode.Txid);
            return;
        }

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
                    (_, oldValue) => Helpers.Round(
                        oldValue - Helpers.Round(
                            oldValue * Helpers.Round(
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
                    v = Helpers.Round(t.Value * Helpers.Round(s.Value / d));

                AddOrUpdateEdge(new S2SEdge(
                    s.Key, t.Key, v,
                    EdgeType.Transfer,
                    Timestamp,
                    Height));
            }

            var x = fee * Helpers.Round(s.Value / sumInputWithoutFee == 0 ? 1 : sumInputWithoutFee);
            if (fee > 0)
                foreach (var m in coinbaseTxG.TargetScripts)
                    AddOrUpdateEdge(new S2SEdge(s.Key, m.Key,
                        Helpers.Round(x * Helpers.Round(m.Value / totalPaidToMiner)),
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

    public new void AddOrUpdateEdge(C2TEdge edge)
    {
        base.AddOrUpdateEdge(edge);
        Stats.IncrementEdgeType(edge.Label, edge.Value);
    }

    public new void AddOrUpdateEdge(C2SEdge edge)
    {
        base.AddOrUpdateEdge(edge);
        Stats.IncrementEdgeType(edge.Label, edge.Value);
    }

    public new void AddOrUpdateEdge(T2TEdge edge)
    {
        base.AddOrUpdateEdge(edge);
        Stats.IncrementEdgeType(edge.Label, edge.Value);
    }
    
    public new void AddOrUpdateEdge(S2SEdge edge)
    {
        base.AddOrUpdateEdge(edge);
        Stats.IncrementEdgeType(edge.Label, edge.Value);
    }

    public bool Equals(BlockGraph? other)
    {
        var equal = base.Equals(other);

        if (!equal)
            return false;

        throw new NotImplementedException();
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
