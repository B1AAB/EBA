using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class BlockGraph : BitcoinGraph, IEquatable<BlockGraph>
{
    public uint Timestamp { get; }
    public Block Block { get; }
    public BlockNode BlockNode { private set; get; }

    /// <summary>
    /// Sets and gets retry attempts to contruct the block graph.
    /// </summary>
    public int Retries { set; get; } = 0;

    public TimeSpan Runtime { get { return _stopwatch.Elapsed; } }
    private readonly Stopwatch _stopwatch = new();
    public void StartStopwatch()
    {
        _stopwatch.Start();
    }
    public void StopStopwatch()
    {
        _stopwatch.Stop();
    }

    private readonly uint[] _edgeLabelCount = new uint[Enum.GetNames<EdgeLabel>().Length];
    private readonly long[] _edgeLabelValueSum = new long[Enum.GetNames<EdgeLabel>().Length];
    public void IncrementEdgeType(EdgeLabel label, long value)
    {
        Interlocked.Increment(ref _edgeLabelCount[(int)label]);
        Helpers.ThreadsafeAdd(ref _edgeLabelValueSum[(int)label], value);
    }

    private TxGraph _coinbaseTxGraph;

    private readonly ConcurrentQueue<TxGraph> _txGraphsQueue = new();

    private readonly ILogger<BitcoinChainAgent> _logger;

    private readonly ChainToGraphModel _chainToGraphModel;

    public BlockGraph(Block block, ChainToGraphModel chainToGraphModel, ILogger<BitcoinChainAgent> logger) : base()
    {
        Block = block;

        // See the following BIP on using `mediantime` instead of `time`.
        // https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
        Timestamp = block.MedianTime;

        _chainToGraphModel = chainToGraphModel;

        _logger = logger;

        StartStopwatch();
    }

    public void SetCoinbaseTx(TxGraph coinbaseTx)
    {
        _coinbaseTxGraph = coinbaseTx;
        Block.SetCoinbaseOutputsCount(coinbaseTx.OutputsCount);
    }

    public void Enqueue(TxGraph g)
    {
        _txGraphsQueue.Enqueue(g);
    }

    public void BuildGraph(CancellationToken ct)
    {
        BlockNode = new BlockNode(Block);
        TryAddNode(BlockNode.ComponentType, BlockNode);

        switch (_chainToGraphModel)
        {
            case ChainToGraphModel.UTxOModel:
                BuildGraphNativeModel(ct);
                break;

            case ChainToGraphModel.AccountModel:
                BuildGraphExpandedModel(ct);
                break;
        }

        BlockNode.EdgeLabelCount = _edgeLabelCount;
        BlockNode.EdgeLabelValueSum = _edgeLabelValueSum;
    }

    private void BuildGraphNativeModel(CancellationToken ct)
    {
        var v = _coinbaseTxGraph.TxNode;
        var t = Timestamp;
        var h = Block.Height;

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

        // Note that the Coinbase tx is processed after all other Txes because 
        // it relies on the fee computed from other Txes.
        long miningReward = 0;
        foreach(var u in _coinbaseTxGraph.OutputScripts)
        {
            AddOrUpdateEdge(
                new T2SEdge(v, new ScriptNode(u.Key), EdgeType.Rewards, t, h, outputs: u.Value),
                (newE, oldE) => T2SEdge.Merge(oldE, newE));

            Block.ProfileCreatedOutput(u.Key, u.Value);
            miningReward += u.Value.Sum(x => x.Value);
        }

        var mintedCoins = miningReward - (long)Block.Fees.Sum;
        Block.SetMintedBitcoins(mintedCoins);
        AddOrUpdateEdge(new C2TEdge(v, mintedCoins, t, h));
        AddOrUpdateEdge(new B2TEdge(BlockNode, v, mintedCoins, EdgeType.Contains, t, h));
    }

    private void AddTxGraphToBlockGraphUsingNativeModel(TxGraph txGraph)
    {
        var v = txGraph.TxNode;
        var h = Block.Height;
        var t = Timestamp;

        foreach (var u in txGraph.SourceTxes)
            AddOrUpdateEdge(new T2TEdge(new TxNode(u.Key), v, u.Value, EdgeType.Transfers, t, h));

        foreach (var u in txGraph.InputScripts)
        {
            var sumValue = u.Value.Sum(x => x.Value);
            AddOrUpdateEdge(
                new S2TEdge(new ScriptNode(u.Key), v, EdgeType.Redeems, t, h, prevOuts: u.Value),
                (newE, oldE) => S2TEdge.Merge(oldE, newE));

            Block.ProfileSpentOutput(u.Key, u.Value);
        }

        foreach (var u in txGraph.OutputScripts)
        {
            AddOrUpdateEdge(
                new T2SEdge(v, new ScriptNode(u.Key), EdgeType.Rewards, t, h, outputs: u.Value),
                (newE, oldE) => T2SEdge.Merge(oldE, newE));

            Block.ProfileCreatedOutput(u.Key, u.Value);
        }

        Block.ProfileTxes(txGraph.InputsCount, txGraph.OutputsCount);
        Block.ProfileFee(txGraph.Fee);
        
        AddOrUpdateEdge(new T2TEdge(v, _coinbaseTxGraph.TxNode, txGraph.Fee, EdgeType.Fee, t, h));
        AddOrUpdateEdge(new B2TEdge(BlockNode, v, txGraph.TotalInputValue, EdgeType.Contains, t, h));
    }

    private void BuildGraphExpandedModel(CancellationToken ct)
    {
        // TODO: make sure this is addressed in the updated method
        var miningReward = 0;//_coinbaseTxGraph.TargetScripts.Sum(x => x.Value);
        var mintedBitcoins = miningReward - (long)Block.Fees.Sum;
        Block.SetMintedBitcoins(mintedBitcoins);
        //Block.SetTxFees(TotalFee); // TEMP disable this part of the code will be deprecated soon


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

        /*
        foreach (var item in _coinbaseTxGraph.TargetScripts)
        {
            AddOrUpdateEdge(new C2SEdge(
                new ScriptNode(item.ScriptPubKey),
                //Helpers.Round(item.Value * (mintedBitcoins / (double)miningReward)),
                Helpers.Round(mintedBitcoins * (item.Value / (double)miningReward)),
                Timestamp,
                Block.Height));
        }*/
    }

    private void AddTxGraphToBlockGraphUsingExpandedModel(
        TxGraph txGraph,
        TxGraph coinbaseTxG,
        long totalPaidToMiner,
        CancellationToken ct)
    {
        // TODO: all the AddOrUpdateEdge methods in the following are all hotspots.
        // VERY IMPORTANT TODO: THIS IS TEMPORARY UNTIL A GOOD SOLUTION IS IMPLEMENTED.

        //var sources = txGraph.SourceScripts; // <---- original,
        var sources = new List<Input>(); // <---- temp

        /*
        if (sources.Count > 20 && txGraph.TargetScripts.Count > 20)
        {
            _logger.LogWarning(
                "Skipping a transaction because it contains more than 20 source and target nodes, " +
                "maximum currently supported. " +
                "Block: {b:n0}; " +
                "source scripts count: {s:n0}; " +
                "target scripts count: {t:n0}; " +
                "transaction hash: {tx}.",
                Block.Height,
                sources.Count,
                txGraph.TargetScripts.Count,
                txGraph.TxNode.Txid);
            return;
        }*/

        var fee = txGraph.Fee;
        if (fee > 0.0)
        {
            foreach (var s in sources)
            {
                var sourceFeeShare = Helpers.Round(fee * (s.PrevOut.Value / (double)(txGraph.TotalInputValue == 0 ? 1 : txGraph.TotalInputValue)));
                /*
                foreach (var minerScript in coinbaseTxG.TargetScripts)
                {
                    throw new NotImplementedException();
                    /*
                    AddOrUpdateEdge(new S2SEdge(s.Key, minerScript.Key,
                        Helpers.Round(sourceFeeShare * (minerScript.Value / (double)totalPaidToMiner)),
                        EdgeType.Fee, Timestamp, Block.Height));*/
                //}

                // TODO: this is temporarily disabled after changing value type and not fixed because this method will be deprecated soon.
                // txGraph.SourceScripts.AddOrUpdate(s.Key, s.Value, (_, preV) => preV.Value - sourceFeeShare);
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
            foreach (var s in sources)
            {
                if (ct.IsCancellationRequested)
                    return;

                //foreach (var t in txGraph.TargetScripts)
                //{
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

                    //throw new NotImplementedException();
                    /*
                    AddOrUpdateEdge(new S2SEdge(
                        s.Key, t.Key, Helpers.Round(t.Value * (s.Value.PrevOut.Value / (double)sumInputWithoutFee)),
                        EdgeType.Transfers,
                        Timestamp,
                        Block.Height));*/
                //}
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

    public void AddOrUpdateEdge<T>(T edge)
        where T: IEdge<Graph.Model.INode, Graph.Model.INode>
    {
        base.AddOrUpdateEdge(edge);
        IncrementEdgeType(edge.Label, edge.Value);
    }

    public void AddOrUpdateEdge(T2TEdge edge)
    {
        AddOrUpdateEdge(edge, T2TEdge.Update);
        IncrementEdgeType(edge.Label, edge.Value);
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
