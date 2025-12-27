using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.Graph;

public class BlockGraph : BitcoinGraph, IEquatable<BlockGraph>
{
    public uint Timestamp { get; }
    public Block Block { get; }
    public BlockNode BlockNode { get; }
    public BlockStatistics Stats { set; get; }

    public List<ScriptNode> RewardsAddresses { set; get; } = [];

    /// <summary>
    /// Is the sum of all the tranactions fee.
    /// </summary>
    public long TotalFee { get { return _totalFee; } }
    private long _totalFee;

    public long MiningReward { private set; get; }
    public long MintedCoins { private set; get; }

    private TransactionGraph _coinbaseTxGraph;

    private readonly ConcurrentQueue<TransactionGraph> _txGraphsQueue = new();

    private readonly ILogger<BitcoinChainAgent> _logger;

    private readonly Lock _feeLock = new();

    private readonly ChainToGraphModel _chainToGraphModel;

    public BlockGraph(Block block, ChainToGraphModel chainToGraphModel, ILogger<BitcoinChainAgent> logger) : base()
    {
        Block = block;
        BlockNode = new BlockNode(block);
        TryAddNode(BlockNode.ComponentType, BlockNode);

        // See the following BIP on using `mediantime` instead of `time`.
        // https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
        Timestamp = block.MedianTime;

        _chainToGraphModel = chainToGraphModel;

        _logger = logger;

        Stats = new BlockStatistics(block);
        Stats.StartStopwatch();
    }

    public void SetCoinbaseTx(TransactionGraph coinbaseTx)
    {
        _coinbaseTxGraph = coinbaseTx;
        MiningReward = _coinbaseTxGraph.TargetScripts.Sum(x => x.Value);

        lock (_feeLock)
        {
            MintedCoins = MiningReward - _totalFee;
        }
    }

    public void Enqueue(TransactionGraph g)
    {
        lock (_feeLock)
        {
            _totalFee += g.Fee;
            MintedCoins = MiningReward - _totalFee;
        }

        _txGraphsQueue.Enqueue(g);
    }

    public void BuildGraph(CancellationToken ct)
      {
        switch(_chainToGraphModel)
        {
            case ChainToGraphModel.UTxOModel:
                BuildGraphNativeModel(ct);
                break;

            case ChainToGraphModel.AccountModel:
                BuildGraphExpandedModel(ct);
                break;
        }
    }

    private void BuildGraphNativeModel(CancellationToken ct)
    {
        foreach (var target in _coinbaseTxGraph.TargetScripts)
        {
            AddOrUpdateEdge(new T2SEdge(
                _coinbaseTxGraph.TxNode,
                target.Key,
                target.Value,
                EdgeType.Rewards,
                Timestamp,
                Block.Height));
        }

        AddOrUpdateEdge(new C2TEdge(_coinbaseTxGraph.TxNode, MintedCoins, Timestamp, Block.Height));
        AddOrUpdateEdge(new B2TEdge(BlockNode, _coinbaseTxGraph.TxNode, MintedCoins, EdgeType.Contains, Timestamp, Block.Height));

        Parallel.ForEach(_txGraphsQueue,
            #if (DEBUG)
            parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = 1 },
            #endif
        (txGraph, state) =>
        {
            if (ct.IsCancellationRequested)
            { state.Stop(); return; }

            AddTxGraphToBlockGraphUsingNativeModel(txGraph);

            if (ct.IsCancellationRequested)
            { state.Stop(); return; }
        });
    }

    private void AddTxGraphToBlockGraphUsingNativeModel(TransactionGraph txGraph)
    {
        foreach (var source in txGraph.SourceScripts)
        {
            AddOrUpdateEdge(new S2TEdge(
                source.Key,
                txGraph.TxNode,
                source.Value,
                EdgeType.Redeems,
                Timestamp,
                Block.Height));
        }

        foreach (var sourceTx in txGraph.SourceTxes)
        {
            var txNode = new TxNode(sourceTx.Key);
            AddOrUpdateEdge(new T2TEdge(
                txNode,
                txGraph.TxNode,
                sourceTx.Value,
                EdgeType.Transfers,
                Timestamp,
                Block.Height));

            // AddOrUpdate(new B2TEdge(BlockNode, txNode, sourceTx.Value, EdgeType.Redeems, Timestamp, Block.Height));
        }

        foreach (var target in txGraph.TargetScripts)
        {
            AddOrUpdateEdge(new T2SEdge(
                txGraph.TxNode,
                target.Key,
                target.Value,
                EdgeType.Rewards,
                Timestamp,
                Block.Height));
        }

        AddOrUpdateEdge(new T2TEdge(
            txGraph.TxNode,
            _coinbaseTxGraph.TxNode,
            txGraph.Fee,
            EdgeType.Fee,
            Timestamp,
            Block.Height));

        AddOrUpdateEdge(new B2TEdge(BlockNode, txGraph.TxNode, txGraph.TotalInputValue, EdgeType.Contains, Timestamp, Block.Height));
    }

    private void BuildGraphExpandedModel(CancellationToken ct)
    {
        // TODO: make sure this is addressed in the updated method
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

                AddTxGraphToBlockGraphUsingExpandedModel(txGraph, _coinbaseTxGraph, miningReward, ct);

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

    private void AddTxGraphToBlockGraphUsingExpandedModel(
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

    public new void AddOrUpdate(S2TEdge edge)
    {
        base.AddOrUpdateEdge(edge);
        Stats.IncrementEdgeType(edge.Label, edge.Value);
    }

    public new void AddOrUpdate(T2SEdge edge)
    {
        base.AddOrUpdateEdge(edge);
        Stats.IncrementEdgeType(edge.Label, edge.Value);
    }

    public new void AddOrUpdate(B2TEdge edge)
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
