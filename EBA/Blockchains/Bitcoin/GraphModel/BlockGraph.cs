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

    public BlockGraph(Block block, ILogger<BitcoinChainAgent> logger) : base()
    {
        Block = block;

        // See the following BIP on using `mediantime` instead of `time`.
        // https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
        Timestamp = block.MedianTime;

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

            AddTxGraphToBlockGraph(txGraph);

            if (ct.IsCancellationRequested)
            { state.Stop(); return; }
        });

        // Note that the Coinbase tx is processed after all other Txes because 
        // it relies on the fee computed from other Txes.
        long miningReward = 0;
        foreach (var u in _coinbaseTxGraph.OutputScripts)
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


        BlockNode.EdgeLabelCount = _edgeLabelCount;
        BlockNode.EdgeLabelValueSum = _edgeLabelValueSum;
    }

    private void AddTxGraphToBlockGraph(TxGraph txGraph)
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
