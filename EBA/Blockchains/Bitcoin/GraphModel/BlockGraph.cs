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

    private readonly ConcurrentDictionary<EdgeKind, uint> _edgeLabelCount = [];
    private readonly ConcurrentDictionary<EdgeKind, long> _edgeLabelValueSum = [];
    public void IncrementEdgeType(EdgeKind edgeKind, long value)
    {
        _edgeLabelCount.AddOrUpdate(edgeKind, 1, (_, oldValue) => oldValue + 1);
        _edgeLabelValueSum.AddOrUpdate(edgeKind, value, (_, oldValue) => oldValue + value);
    }

    private TxGraph _coinbaseTxGraph;

    private readonly ConcurrentQueue<TxGraph> _txGraphsQueue = new();

    private readonly ILogger<BitcoinChainAgent> _logger;

    public BlockGraph(string id, Block block, ILogger<BitcoinChainAgent> logger) : base(id)
    {
        Block = block;

        // See the following BIP on using `mediantime` instead of `time`.
        // https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
        Timestamp = block.MedianTime;

        _logger = logger;

        foreach(var kind in Schema.EdgeKinds)
        {
            _edgeLabelCount.TryAdd(kind, 0);
            _edgeLabelValueSum.TryAdd(kind, 0);
        }

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
        TryAddNode(BlockNode);

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
        foreach (var u in _coinbaseTxGraph.Outputs)
        {
            TryGetOrAddEdge(new T2SEdge(v, new ScriptNode(u.ScriptPubKey), t, h, output: u), out var edge);

            Block.ProfileCreatedOutput(u);
            miningReward += u.Value;
        }

        var mintedCoins = miningReward - (long)Block.FeesStats.Sum;
        Block.SetMintedBitcoins(mintedCoins);
        AddOrUpdateEdge(new C2TEdge(v, mintedCoins, t, h));
        AddOrUpdateEdge(new B2TEdge(BlockNode, v, mintedCoins, t, h));

        BlockNode.TripletTypeCount = _edgeLabelCount.ToDictionary(x => x.Key, x => x.Value);
        BlockNode.TripletTypeValueSum = _edgeLabelValueSum.ToDictionary(x => x.Key, x => x.Value);
    }

    private void AddTxGraphToBlockGraph(TxGraph txGraph)
    {
        var v = txGraph.TxNode;
        var h = Block.Height;
        var t = Timestamp;

        foreach (var u in txGraph.SourceTxes)
            AddOrUpdateEdge(new T2TEdge(new TxNode(u.Key), v, u.Value, RelationType.Transfers, t, h));

        foreach (var u in txGraph.Inputs)
        {
            TryGetOrAddEdge(
                new S2TEdge(new ScriptNode(u.PrevOut.ScriptPubKey), v, t, h, u),
                out var edge);

            Block.ProfileSpentOutput(u);
        }

        foreach (var u in txGraph.Outputs)
        {
            TryGetOrAddEdge(new T2SEdge(v, new ScriptNode(u.ScriptPubKey), t, h, output: u), out var edge);

            Block.ProfileCreatedOutput(u);
        }

        Block.ProfileTxes(txGraph.InputsCount, txGraph.OutputsCount);
        Block.ProfileFee(txGraph.Fee);
        
        AddOrUpdateEdge(new T2TEdge(v, _coinbaseTxGraph.TxNode, txGraph.Fee, RelationType.Fee, t, h));
        AddOrUpdateEdge(new B2TEdge(BlockNode, v, txGraph.TotalInputValue, t, h));
    }

    public void AddOrUpdateEdge<T>(T edge)
        where T: IEdge<Graph.Model.INode, Graph.Model.INode>
    {
        base.AddOrUpdateEdge(edge);
        IncrementEdgeType(edge.EdgeKind, edge.Value);
    }

    public void AddOrUpdateEdge(T2TEdge edge)
    {
        AddOrUpdateEdge(edge, T2TEdge.Update);
        IncrementEdgeType(edge.EdgeKind, edge.Value);
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
