using BC2G.Utilities;

namespace BC2G.Blockchains.Bitcoin.Graph;

public class BlockGraph : BitcoinGraph, IEquatable<BlockGraph>
{
    public uint Timestamp { get; }
    public Block Block { get; }
    public BlockStatistics Stats { set; get; }

    public List<ScriptNode> RewardsAddresses { set; get; } = [];

    /// <summary>
    /// Is the sum of all the tranactions fee.
    /// </summary>
    public long TotalFee { get { return _totalFee; } }
    private long _totalFee;

    private readonly TransactionGraph _coinbaseTxGraph;

    private readonly ConcurrentQueue<TransactionGraph> _txGraphsQueue = new();

    private readonly ILogger<BitcoinAgent> _logger;

    public BlockGraph(Block block, TransactionGraph coinbaseTxGraph, ILogger<BitcoinAgent> logger) : base()
    {
        Block = block;
        _coinbaseTxGraph = coinbaseTxGraph;
        _logger = logger;

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
        var miningReward = _coinbaseTxGraph.TargetScripts.Sum(x => x.Value);
        var mintedBitcoins = miningReward - TotalFee;

        Stats.MintedBitcoins = mintedBitcoins;
        Stats.TxFees = TotalFee;

        AddOrUpdateEdge(new C2TEdge(_coinbaseTxGraph.TxNode, mintedBitcoins, Timestamp, Block.Height));

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
                //Helpers.Round(item.Value * (mintedBitcoins / (double)miningReward)),
                Helpers.Round(mintedBitcoins * (item.Value / (double)miningReward)),
                Timestamp,
                Block.Height));
        }
    }

    private void Merge(
        TransactionGraph txGraph,
        TransactionGraph coinbaseTxG,
        long totalPaidToMiner,
        CancellationToken ct)
    {
        // TODO: all the AddOrUpdateEdge methods in the following are all hotspots.
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
                Block.Height,
                txGraph.SourceScripts.Count,
                txGraph.TargetScripts.Count,
                txGraph.TxNode.Txid);
            return;
        }

        var fee = txGraph.Fee;
        if (fee > 0.0)
        {
            foreach (var s in txGraph.SourceScripts)
            {
                var sourceFeeShare = Helpers.Round(fee * (s.Value / (double)(txGraph.TotalInputValue == 0 ? 1 : txGraph.TotalInputValue)));

                foreach (var minerScript in coinbaseTxG.TargetScripts)
                {
                    AddOrUpdateEdge(new S2SEdge(s.Key, minerScript.Key,
                        Helpers.Round(sourceFeeShare * (minerScript.Value / (double)totalPaidToMiner)),
                        EdgeType.Fee, Timestamp, Block.Height));
                }

                txGraph.SourceScripts.AddOrUpdate(s.Key, s.Value, (_, preV) => preV - sourceFeeShare);
            }

            AddOrUpdateEdge(new T2TEdge(txGraph.TxNode, coinbaseTxG.TxNode, fee, EdgeType.Fee, Timestamp, Block.Height));
        }

        var sumInputWithoutFee = txGraph.TotalInputValue - fee;

        /*
         * TODO: currently we do not skip the self-transfer transactions.
         * If you want to skip these, a code like the following should be 
         * implemented, additionally, a similar modification on the Tx 
         * should also implemented where it subtracts the values of 
         * script-to-script transfers to be skipped from the total value 
         * of Tx-to-Tx transfers. 
         * Note that it can be tricky, since if you subtract the self-transfer 
         * from a Tx, then, for the source Tx of the Tx with self-transfer,
         * it will seem as if the received value of a Tx is more than the value it spent.
         * 
        foreach (var s in txGraph.SourceScripts)
            foreach (var t in txGraph.TargetScripts)
                if (s.Key.Address == t.Key.Address)
                {
                    txGraph.SourceScripts.AddOrUpdate(s.Key, s.Value, (_, preV) => preV - t.Value);
                    sumInputWithoutFee -= t.Value;
                }
        */

        if (sumInputWithoutFee == 0)
        {
            _logger.LogInformation(
                "Sum of input without fee is zero, skipping all the script-to-script transfers. " +
                "Tx ID: {txid}", txGraph.TxNode.Txid);
        }
        else
        {
            foreach (var s in txGraph.SourceScripts)
            {
                if (ct.IsCancellationRequested)
                    return;

                foreach (var t in txGraph.TargetScripts)
                {
                    /* 
                     * See above comment for context.
                     * 
                     * It means the transaction is a "change" transfer 
                     * (i.e., return the remainder of a transfer to self),
                     * we avoid these transactions to simplify graph representation. 
                     * 
                    if (s.Key.Address == t.Key.Address)
                        continue;
                    */

                    AddOrUpdateEdge(new S2SEdge(
                        s.Key, t.Key, Helpers.Round(t.Value * (s.Value / (double)sumInputWithoutFee)),
                        EdgeType.Transfers,
                        Timestamp,
                        Block.Height));
                }
            }
        }

        if (ct.IsCancellationRequested)
            return;

        foreach (var tx in txGraph.SourceTxes)
        {
            if (tx.Key == txGraph.TxNode.Id)
            {
                // TODO: Not sure if this condition ever happens.
                _logger.LogWarning(
                    "Skipping creating a T2T edge since the source and target Tx IDs are identical." +
                    "Source Tx ID={source_txid}, Target Tx ID={target_txid}",
                    txGraph.TxNode.Id, tx.Key);

                continue;
            }

            AddOrUpdateEdge(new T2TEdge(
                new TxNode(tx.Key), txGraph.TxNode, tx.Value, EdgeType.Transfers, Timestamp, Block.Height));
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
