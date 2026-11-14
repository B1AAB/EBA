using AAB.EBA.Graph.Bitcoin.Descriptors;

namespace AAB.EBA.Graph.Bitcoin.TraversalAlgorithms;

// TODO: this is an experimental implementation, it has a lot of patches and hotfixes.
// We continue with these patches for now until we converge on how this sampling algorithm should function. 
// Then, this class should be thoroughly reimplemented.

public class Panorama : ITraversalAlgorithm
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<BitcoinGraphOrchestrator> _logger;

    private static readonly PropertyMapping<TxNode> _txidMapping = TxNodeDescriptor.StaticMapper.GetMapping(x => x.Txid);
    private static readonly PropertyMapping<BlockNode> _heightMapping = BlockNodeDescriptor.StaticMapper.GetMapping(x => x.BlockMetadata.Height);

    // Relationship property names holding block heights, per relationship type.
    // Redeems (S2T) and Credits (T2S) carry both a creation and a spent height;
    // B2TEdge and C2TEdge map their height under the same `Height` name as T2TEdge,
    // so that single name covers Transfers, Fee, Confirms, and Mints.
    private static readonly string[] _redeemsHeightProperties =
    [
        S2TEdgeDescriptor.StaticMapper.GetMapping(x => x.CreationHeight).Property.Name,
        S2TEdgeDescriptor.StaticMapper.GetMapping(x => x.SpentHeight).Property.Name,
    ];
    private static readonly string[] _creditsHeightProperties =
    [
        T2SEdgeDescriptor.StaticMapper.GetMapping(x => x.CreationHeight).Property.Name,
        T2SEdgeDescriptor.StaticMapper.GetMapping(x => x.SpentHeight).Property.Name,
    ];
    private static readonly string[] _defaultHeightProperties =
    [
        T2TEdgeDescriptor.StaticMapper.GetMapping(x => x.Height).Property.Name,
    ];

    // T2SEdge stores this sentinel as SpentHeight while the output is unspent.
    private const long _unspentSentinel = long.MaxValue;

    // A database step that runs longer than this is reported (once at the threshold, then
    // every repeat interval) naming the step, so a stall can be attributed from the log
    // without logging every step. The report comes from an independent timer thread, so
    // it fires even if the step blocks its own thread synchronously.
    private static readonly TimeSpan _slowStepThreshold = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _slowStepRepeat = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _slowStepPoll = TimeSpan.FromSeconds(5);

    private readonly object _stepLock = new();
    private string? _currentStep;
    private readonly Stopwatch _currentStepWatch = new();
    private TimeSpan _currentStepLastReport;

    // Progress of the neighborhood walk in progress (one root). A walk made of thousands
    // of individually fast steps produces no log at all between its start and end lines,
    // so the same timer reports a one-line progress summary at this interval while a walk
    // is running, and a one-line total when a walk ran longer than the slow-step threshold.
    private static readonly TimeSpan _walkProgressInterval = TimeSpan.FromSeconds(60);
    private string? _walkRoot;
    private readonly Stopwatch _walkWatch = new();
    private TimeSpan _walkLastReport;
    private int _walkQueries;
    private int _walkHop;
    private int _walkQueue;
    private int _walkNodes;
    private int _walkEdges;

    private int _maxHopReached = 0;

    private readonly bool _hasEdgeActivityWindow;
    private readonly long _minEdgeActivityHeight;
    private readonly long _maxEdgeActivityHeight;

    private readonly ActivityWindowOptions? _incomingActivityWindow;
    private readonly ActivityWindowOptions? _outgoingActivityWindow;
    private readonly ActivityWindowOptions? _totalActivityWindow;
    private readonly bool _hasActivityWindow;

    // Ranges sent to the DB, in a fixed order: [incoming, outgoing, total]
    private readonly List<BlockHeightRange> _activityRanges = [];
    private readonly int _incomingRangeIndex = -1;
    private readonly int _outgoingRangeIndex = -1;
    private readonly int _totalRangeIndex = -1;

    // Per sampled graph (reset in GetNeighborsAsync).
    private readonly Dictionary<string, bool> _activityCheckCache = [];
    private int _activityChecks = 0;
    private int _activityRejections = 0;
    private int _activityPartialScans = 0;
    private int _scriptTerminalPrunes = 0;

    public Panorama(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphOrchestrator> logger)
    {
        _options = options;
        _graphDb = graphDb;
        _logger = logger;

        var po = _options.Bitcoin.GraphSample.PanoramaOptions;
        _minEdgeActivityHeight = po.MinEdgeActivityHeight;
        _maxEdgeActivityHeight = po.MaxEdgeActivityHeight;
        _hasEdgeActivityWindow = po.MinEdgeActivityHeight > 0 || po.MaxEdgeActivityHeight < int.MaxValue;

        _incomingActivityWindow = po.IncomingActivityWindow;
        _outgoingActivityWindow = po.OutgoingActivityWindow;
        _totalActivityWindow = po.TotalActivityWindow;
        _hasActivityWindow =
            _incomingActivityWindow != null ||
            _outgoingActivityWindow != null ||
            _totalActivityWindow != null;

        if (_incomingActivityWindow != null)
        {
            _incomingRangeIndex = _activityRanges.Count;
            _activityRanges.Add(new BlockHeightRange(_incomingActivityWindow.MinHeight, _incomingActivityWindow.MaxHeight));
        }
        if (_outgoingActivityWindow != null)
        {
            _outgoingRangeIndex = _activityRanges.Count;
            _activityRanges.Add(new BlockHeightRange(_outgoingActivityWindow.MinHeight, _outgoingActivityWindow.MaxHeight));
        }
        if (_totalActivityWindow != null)
        {
            _totalRangeIndex = _activityRanges.Count;
            _activityRanges.Add(new BlockHeightRange(_totalActivityWindow.MinHeight, _totalActivityWindow.MaxHeight));
        }
    }

    public async Task SampleAsync(CancellationToken ct)
    {
        var sampledSubGraphsCount = 0;
        var attempts = 0;

        _logger.LogInformation("Sampling {n:N0} graphs.", _options.Bitcoin.GraphSample.Count - sampledSubGraphsCount);

        if (_options.Bitcoin.GraphSample.PanoramaOptions.EnsureScriptTerminalNodes)
            _logger.LogInformation(
                "Pruning expansions that cannot reach a Script node on the final layer (ensureScriptTerminalNodes).");

        JsonSerializer<FeaturesSchema>.Serialize(new FeaturesSchema(), Path.Combine(_options.WorkingDir, "schema.json"));

        // Reports the step currently in progress once it has run longer than the slow-step
        // threshold; see WatchSlowStepAsync.
        using var slowStepTimer = new Timer(_ => ReportSlowStep(), null, _slowStepPoll, _slowStepPoll);

        while (
            sampledSubGraphsCount < _options.Bitcoin.GraphSample.Count &&
            ++attempts <= _options.Bitcoin.GraphSample.MaxAttempts)
        {
            var remaining = _options.Bitcoin.GraphSample.Count - sampledSubGraphsCount;
            _logger.LogInformation(
                "Attempt {Attempt}/{MaxAttempts}: querying {remaining:N0} random root node(s) for sampling.",
                attempts, _options.Bitcoin.GraphSample.MaxAttempts, remaining);

            var rndRootNodes = await GetRootNodesAsync(remaining, ct);

            _logger.LogInformation(
                "Attempt {Attempt}: Selected {r:N0} random root node(s).",
                attempts, rndRootNodes.Count);

            int counter = 0;
            foreach (var rootNode in rndRootNodes)
            {
                ct.ThrowIfCancellationRequested();
                var rootNodeLabelInLogs = $"({rootNode.Kind} {{{rootNode.IdProperty}={rootNode.Id}}})";

                _logger.LogInformation(
                    "Sampling neighbors for root {Index:N0}/{Total:N0}. {rootNodeTag}",
                    ++counter, rndRootNodes.Count, rootNodeLabelInLogs);

                var graph = await GetNeighborsAsync(
                    rootNodeLabel: rootNode.Kind,
                    rootNodeIdProperty: rootNode.IdProperty,
                    rootNodeId: rootNode.Id,
                    ct: ct);

                var perBatchLabelsFilename = Path.Join(_options.WorkingDir, "labels.tsv");

                if (graph.NodeCount < _options.Bitcoin.GraphSample.MinNodeCount ||
                    graph.EdgeCount < _options.Bitcoin.GraphSample.MinEdgeCount)
                {
                    _logger.LogWarning(
                        "Sampled neighborhood of the root {rootNodeTag} is rejected because the sampled graph size did not meet constraints: " +
                        "nodes={NodeCount:N0} (min={MinNode:N0}, max={MaxNode:N0}), edges={EdgeCount:N0} (min={MinEdge:N0}, max={MaxEdge:N0}).",
                        rootNodeLabelInLogs,
                        graph.NodeCount,
                        _options.Bitcoin.GraphSample.MinNodeCount,
                        _options.Bitcoin.GraphSample.MaxNodeCount,
                        graph.EdgeCount,
                        _options.Bitcoin.GraphSample.MinEdgeCount,
                        _options.Bitcoin.GraphSample.MaxEdgeCount);
                }
                else
                {
                    ct.ThrowIfCancellationRequested();

                    var graphDir = Path.Join(_options.WorkingDir, graph.Id);
                    graph.Serialize(graphDir, perBatchLabelsFilename);

                    _logger.LogInformation(
                        "Successfully serialized sampled graph from root {rootNodeTag}",
                        rootNodeLabelInLogs);

                    sampledSubGraphsCount++;

                    _logger.LogInformation(
                        "Sampled graphs so far: {successful:N0}/{TotalRequested:N0}.",
                        sampledSubGraphsCount,
                        _options.Bitcoin.GraphSample.Count);
                }
            }
        }

        _logger.LogInformation(
            "Sampling finished. Total sampled communities={TotalSampled:N0}, Attempts={Attempts}.",
            sampledSubGraphsCount, attempts);
    }

    /// <summary>
    /// Samples a subcommunity from the provided neighborhood query results, 
    /// adds the corresponding nodes and edges to the specified Bitcoin graph, 
    /// and returns the list of nodes added during this operation.
    /// This method ensures that only unique nodes and edges not already present in the graph are added. 
    /// The number of nodes sampled at each hop decreases linearly based on the specified reduction factor.
    /// </summary>
    private async Task<List<Model.INode>> ProcessNeighborhoodAsync(
        List<IRecord> samplingResult,
        int hop,
        BitcoinGraph g,
        CancellationToken ct)
    {
        var nodesAddedToGraph = new List<Model.INode>();
        if (samplingResult.Count == 0)
            return nodesAddedToGraph;

        var nodesUniqueToThisHop = new Dictionary<string, Model.INode>();
        var nodesInThisHopAlreadyInGraph = new Dictionary<string, Model.INode>();
        var edges = new List<IRelationship>();

        var rootList = samplingResult[0]["root"].As<List<object>>();
        if (!TryUnpackNodeDict(rootList[0].As<IDictionary<string, object>>(), hop, out var builtParentNode) || builtParentNode == null)
            return nodesAddedToGraph;

        // Must run before the root is added to the graph.
        if (hop == 0 && _hasActivityWindow && !await CheckScriptActivityAsync(builtParentNode, ct))
        {
            _logger.LogInformation(
                "Root node {Id} rejected: its activity does not satisfy the configured activity window(s).",
                builtParentNode.Id);
            return nodesAddedToGraph; // empty; the sample fails MinNodeCount and is retried
        }

        Model.INode parentNode = g.GetOrAddNode(builtParentNode);

        if (hop == 0)
            g.AddLabel("RootNodeId", parentNode.Id);

        var coinbaseNodeIdInGraphDb = "";
        for (var i = 0; i < samplingResult.Count; i++)
        {
            var r = samplingResult[i];
            foreach (var nodeObject in r["nodes"].As<List<object>>())
            {
                if (!TryUnpackNodeDict(nodeObject.As<IDictionary<string, object>>(), hop, out var node)
                    || node == null
                    || node.IdInGraphDb == null)
                    continue;

                if (g.TryGetNode(node.Id, out var nodeInG))
                {
                    nodesInThisHopAlreadyInGraph.TryAdd(node.IdInGraphDb, nodeInG);
                }
                else
                {
                    nodesUniqueToThisHop.TryAdd(node.IdInGraphDb, node);

                    if (node.NodeKind == NodeKind.Coinbase)
                        coinbaseNodeIdInGraphDb = node.IdInGraphDb;
                }
            }

            foreach (var edge in r.Values["relationships"].As<List<IRelationship>>())
            {
                // When an edge-activity height window is set, only edges whose activity height is
                // inside the window are kept. Note the window is applied *after* the
                // query's `queryLimit`, so the candidate pool is "the first queryLimit
                // neighbors visited, then filtered"; raise queryLimit when sampling
                // with narrow windows on high-degree nodes.
                if (!IsWithinEdgeActivityWindow(edge))
                    continue;

                edges.Add(edge);
            }
        }

        if (_hasEdgeActivityWindow)
        {
            // Keep only candidate nodes that are an endpoint of at least one in-window
            // edge to the parent; otherwise a node with no surviving edge could be drawn
            // into nodesToKeep and waste a slot without ever being added to the graph.
            // Only edges incident to the parent count: subgraphAll also returns
            // neighbor-to-neighbor relationships (e.g. a Transfers edge between two
            // Tx neighbors of a Script root), and the edge loop below ignores those.
            var inWindowEndpoints = new HashSet<string>(edges.Count * 2);
            foreach (var edge in edges)
            {
                if (edge.StartNodeElementId != parentNode.IdInGraphDb &&
                    edge.EndNodeElementId != parentNode.IdInGraphDb)
                    continue;

                inWindowEndpoints.Add(edge.StartNodeElementId);
                inWindowEndpoints.Add(edge.EndNodeElementId);
            }

            foreach (var id in nodesUniqueToThisHop.Keys.Where(k => !inWindowEndpoints.Contains(k)).ToList())
                nodesUniqueToThisHop.Remove(id);

            foreach (var id in nodesInThisHopAlreadyInGraph.Keys.Where(k => !inWindowEndpoints.Contains(k)).ToList())
                nodesInThisHopAlreadyInGraph.Remove(id);

            if (!string.IsNullOrEmpty(coinbaseNodeIdInGraphDb) && !inWindowEndpoints.Contains(coinbaseNodeIdInGraphDb))
                coinbaseNodeIdInGraphDb = "";
        }

        ct.ThrowIfCancellationRequested();

        var parentNodeDegree = (parentNode.OriginalInDegree ?? 0) + (parentNode.OriginalOutDegree ?? 0);
        var samplingPercentage =
            _options.Bitcoin.GraphSample.PanoramaOptions.NodeSamplingPercentageAtRoot -
            (hop * _options.Bitcoin.GraphSample.PanoramaOptions.NeighborhoodSamplePercentagePerHop);
        var neighborsToKeep = (int)Math.Floor((parentNodeDegree * samplingPercentage) / 100.0);
        var count = Math.Min(
            _options.Bitcoin.GraphSample.PanoramaOptions.MaxNeighborsPerNode,
            neighborsToKeep - nodesInThisHopAlreadyInGraph.Count);

        var shuffled = ShuffleCandidates(nodesUniqueToThisHop.Keys);

        HashSet<string> nodesToKeep;
        if (!_hasActivityWindow)
        {
            nodesToKeep = shuffled.Take(count).ToHashSet();
        }
        else
        {
            // Walk the shuffled pool and keep the first `count` candidates that satisfy the
            // configured activity window(s). Equivalent to filtering the pool and then drawing
            // `count` at random, but issues one activity-count query per *examined* Script
            // instead of one per pool member. Non-Script candidates always pass.
            nodesToKeep = [];
            foreach (var id in shuffled)
            {
                if (nodesToKeep.Count >= count)
                    break;

                ct.ThrowIfCancellationRequested();

                if (await CheckScriptActivityAsync(nodesUniqueToThisHop[id], ct))
                    nodesToKeep.Add(id);
            }
        }

        if (_options.Bitcoin.GraphSample.PanoramaOptions.ForceIncludeCoinbaseNode &&
            !string.IsNullOrEmpty(coinbaseNodeIdInGraphDb))
        {
            if (!nodesToKeep.Contains(coinbaseNodeIdInGraphDb))
            {
                nodesToKeep.Remove(nodesToKeep.First());
                nodesToKeep.Add(coinbaseNodeIdInGraphDb);
            }
        }

        foreach (var edge in edges)
        {
            if (g.NodeCount >= _options.Bitcoin.GraphSample.MaxNodeCount
                || g.EdgeCount >= _options.Bitcoin.GraphSample.MaxEdgeCount)
            {
                // Reached maximum node or edge count; stopping further additions.
                break;
            }

            string subjectNodeGraphDbId;
            if (edge.StartNodeElementId == parentNode.IdInGraphDb)
                subjectNodeGraphDbId = edge.EndNodeElementId;
            else if (edge.EndNodeElementId == parentNode.IdInGraphDb)
                subjectNodeGraphDbId = edge.StartNodeElementId;
            else
                continue; // edge is not connected to rootNode

            // so only the "connected" nodes are added.
            // the following order where 1st the node is added and then the edge, is important.
            Model.INode? subjectNode = null;
            if (nodesToKeep.Contains(subjectNodeGraphDbId))
            {
                subjectNode = g.GetOrAddNode(nodesUniqueToThisHop[subjectNodeGraphDbId]);
                nodesAddedToGraph.Add(subjectNode);
            }
            else if (nodesInThisHopAlreadyInGraph.TryGetValue(subjectNodeGraphDbId, out subjectNode))
            { }
            else
            {
                continue; // node is not selected to be kept
            }

            IEdge<Model.INode, Model.INode> candidateEdge =
                edge.StartNodeElementId == parentNode.IdInGraphDb ?
                _graphDb.StrategyFactory.CreateEdge(parentNode, subjectNode, edge) :
                _graphDb.StrategyFactory.CreateEdge(subjectNode, parentNode, edge);
            g.TryGetOrAddEdge(candidateEdge, out candidateEdge);
        }

        return nodesAddedToGraph;
    }

    private List<string> ShuffleCandidates(IEnumerable<string> candidates)
    {
        var random = _options.Bitcoin.GraphSample.PanoramaOptions.DeterministicSampling
            ? new Random(31)
            : new Random();
        return candidates.OrderBy(_ => random.Next()).ToList();
    }

    private async Task<bool> ProcessHopsBFS(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int initialHop,
        BitcoinGraph g,
        CancellationToken ct)
    {
        var queue = new Queue<(NodeKind Label, string IdProperty, string Id, int Hop)>();
        queue.Enqueue((rootNodeLabel, rootNodeIdProperty, rootNodeId, initialHop));

        var expandedNodes = new HashSet<string> { rootNodeId };

        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var currentNode = queue.Dequeue();
            _maxHopReached = Math.Max(_maxHopReached, currentNode.Hop);

            var rawNeighborhood = await WatchSlowStepAsync(
                () => _graphDb.GetNeighborhoodAsync(
                    currentNode.Label,
                    currentNode.IdProperty,
                    currentNode.Id,
                    _options.Bitcoin.GraphSample.PanoramaOptions.QueryLimit,
                    1,
                    true,
                    ct: ct),
                $"neighborhood query for ({currentNode.Label} {{{currentNode.IdProperty}={currentNode.Id}}}) at hop {currentNode.Hop}");

            var nodesAddedToGraph = await ProcessNeighborhoodAsync(
                rawNeighborhood,
                currentNode.Hop,
                g: g,
                ct: ct);

            lock (_stepLock)
            {
                _walkHop = currentNode.Hop;
                _walkQueue = queue.Count;
                _walkNodes = g.NodeCount;
                _walkEdges = g.EdgeCount;
            }

            if (g.NodeCount >= _options.Bitcoin.GraphSample.MaxNodeCount ||
                g.EdgeCount >= _options.Bitcoin.GraphSample.MaxEdgeCount)
            {
                return false;
            }

            if (currentNode.Hop < _options.Bitcoin.GraphSample.PanoramaOptions.MaxHops)
            {
                foreach (var node in nodesAddedToGraph)
                {
                    // Note that the `node` is already in the graph, we are just skipping expanding to its neighbors
                    if (node.NodeKind == NodeKind.Block || 
                        node.NodeKind == NodeKind.Coinbase)
                    {
                        continue;
                    }

                    // Note that the `node` is already in the graph, we are just skipping expanding to its neighbors
                    if ((node.OriginalInDegree != null && node.OriginalInDegree > _options.Bitcoin.GraphSample.PanoramaOptions.MaxOriginalInDegree) ||
                        (node.OriginalOutDegree != null && node.OriginalOutDegree > _options.Bitcoin.GraphSample.PanoramaOptions.MaxOriginalOutDegree))
                    {
                        continue;
                    }

                    // Note that the `node` is already in the graph, we are just skipping expanding to its neighbors:
                    // from here, the remaining hops cannot reach a Script node (e.g. a Script at the last hop
                    // only has Tx neighbors).
                    if (_options.Bitcoin.GraphSample.PanoramaOptions.EnsureScriptTerminalNodes &&
                        !CanStillReachScript(node.NodeKind, currentNode.Hop + 1))
                    {
                        _scriptTerminalPrunes++;
                        continue;
                    }

                    // prevent expanding the same node twice
                    if (expandedNodes.Add(node.Id))
                    {
                        queue.Enqueue((node.NodeKind, node.GetIdPropertyName(), node.Id, currentNode.Hop + 1));
                    }
                }
            }
        }

        return true;
    }

    // Smallest number of hops from a node of this kind to a Script node, derived from the
    // graph schema: Script–Tx (Redeems/Credits), Tx–Tx (Transfers/Fee), Block–Tx (Confirms),
    // Coinbase–Tx (Mints), Block–Block (Follows). Only Script and Tx are ever expanded, but
    // the table is complete so the reasoning is explicit.
    private static int MinHopsToScript(NodeKind kind) => kind switch
    {
        NodeKind.Tx => 1,
        NodeKind.Script => 2,   // Script -> Tx -> Script
        NodeKind.Block => 2,    // Block -> Tx -> Script
        NodeKind.Coinbase => 2, // Coinbase -> Tx -> Script
        _ => int.MaxValue,
    };

    // True when expanding `kind` at `hop` can still put a Script on the final layer of the walk.
    // A node dequeued at `hop` adds MaxHops - hop + 1 more layers below it (see ProcessHopsBFS).
    private bool CanStillReachScript(NodeKind kind, int hop) =>
        _options.Bitcoin.GraphSample.PanoramaOptions.MaxHops - hop + 1 >= MinHopsToScript(kind);

    private async Task<bool> ProcessHopsDFS(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int hop,
        BitcoinGraph g,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var samplingResult = await WatchSlowStepAsync(
            () => _graphDb.GetNeighborhoodAsync(
                rootNodeLabel,
                rootNodeIdProperty,
                rootNodeId,
                _options.Bitcoin.GraphSample.PanoramaOptions.QueryLimit,
                1,
                true,
                ct: ct),
            $"neighborhood query for ({rootNodeLabel} {{{rootNodeIdProperty}={rootNodeId}}}) at hop {hop}");

        var selectedNodes = await ProcessNeighborhoodAsync(
            samplingResult, hop,
            g: g,
            ct: ct);

        if (hop < _options.Bitcoin.GraphSample.PanoramaOptions.MaxHops)
        {
            _maxHopReached = Math.Max(_maxHopReached, hop);

            foreach (var node in selectedNodes)
            {
                if (g.NodeCount >= _options.Bitcoin.GraphSample.MaxNodeCount
                    || g.EdgeCount >= _options.Bitcoin.GraphSample.MaxEdgeCount)
                {
                    // Reached maximum node or edge count; stopping further expansions.
                    return false;
                }

                if ((node.OriginalInDegree != null && node.OriginalInDegree > _options.Bitcoin.GraphSample.PanoramaOptions.MaxOriginalInDegree) ||
                    (node.OriginalOutDegree != null && node.OriginalOutDegree > _options.Bitcoin.GraphSample.PanoramaOptions.MaxOriginalOutDegree))
                {
                    // skipping nodes that may belong to exchanges or mixers.
                    // TODO: this is temporary. if this is mixer, then there could be
                    // really informative information in the mixer (e.g., other scripts that usually 2 hop neighborhood from each other)
                    continue;
                }

                if (_options.Bitcoin.GraphSample.PanoramaOptions.EnsureScriptTerminalNodes &&
                    !CanStillReachScript(node.NodeKind, hop + 1))
                {
                    _scriptTerminalPrunes++;
                    continue;
                }

                await ProcessHopsDFS(
                    rootNodeLabel: node.NodeKind,
                    rootNodeIdProperty: node.GetIdPropertyName(),
                    rootNodeId: node.Id,
                    hop: hop + 1,
                    g: g,
                    ct: ct);
            }
        }

        return true;
    }

    private async Task<GraphBase> GetNeighborsAsync(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var g = new BitcoinGraph();

        var rootNodeLabelInLogs = $"({rootNodeLabel} {{{rootNodeIdProperty}={rootNodeId}}})";
        _logger.LogInformation(
            "Retrieving neighbors for root node {rootNodeTag} up to {MaxHops} hop(s). ",
            rootNodeLabelInLogs,
            _options.Bitcoin.GraphSample.PanoramaOptions.MaxHops);

        if (_hasEdgeActivityWindow)
            _logger.LogInformation(
                "Restricting sampled edges to block heights [{Min:N0}, {Max:N0}] (inclusive).",
                _minEdgeActivityHeight, _maxEdgeActivityHeight);

        if (_hasActivityWindow)
            _logger.LogInformation(
                "Script activity window(s) active: incoming={In}, outgoing={Out}, total={Total}; at most {ScanLimit:N0} edges scanned per Script.",
                Describe(_incomingActivityWindow),
                Describe(_outgoingActivityWindow),
                Describe(_totalActivityWindow),
                _options.Bitcoin.GraphSample.PanoramaOptions.ActivityScanLimit);

        _activityCheckCache.Clear();
        _activityChecks = 0;
        _activityRejections = 0;
        _activityPartialScans = 0;
        _scriptTerminalPrunes = 0;

        lock (_stepLock)
        {
            _walkRoot = rootNodeLabelInLogs;
            _walkLastReport = TimeSpan.Zero;
            _walkQueries = 0;
            _walkHop = 0;
            _walkQueue = 0;
            _walkNodes = 0;
            _walkEdges = 0;
            _walkWatch.Restart();
        }

        _maxHopReached = 0;
        var completedWalk = await ProcessHopsBFS(
            rootNodeLabel: rootNodeLabel,
            rootNodeIdProperty: rootNodeIdProperty,
            rootNodeId: rootNodeId,
            initialHop: 0,
            g: g,
            ct: ct);

        if (_options.Bitcoin.GraphSample.PanoramaOptions.IncludeB2TEdges)
            await EnsureB2T(g, ct);

        if (_options.Bitcoin.GraphSample.PanoramaOptions.EarlierBlocksWindow > 0 ||
            _options.Bitcoin.GraphSample.PanoramaOptions.LaterBlocksWindow > 0)
            await EnsureB2BWindow(g, ct);

        if (!completedWalk)
        {
            _logger.LogWarning(
                "Neighbor expansion stopped early because the graph size passed the set limits. " +
                "Current graph size: " +
                "nodes={NodeCount:N0} (max={MaxNodeCount:N0}), " +
                "edges={EdgeCount:N0} (max={MaxEdgeCount:N0}). " +
                "Max hop reached: {maxHop}",
                g.NodeCount,
                _options.Bitcoin.GraphSample.MaxNodeCount,
                g.EdgeCount,
                _options.Bitcoin.GraphSample.MaxEdgeCount,
                _maxHopReached);
        }

        _logger.LogInformation(
            "Neighbor retrieval completed for root node {rootNodeTag}. Built graph with {NodeCount:N0} nodes and {EdgeCount:N0} edges.",
            rootNodeLabelInLogs, g.NodeCount, g.EdgeCount);

        if (_options.Bitcoin.GraphSample.PanoramaOptions.EnsureScriptTerminalNodes)
            _logger.LogInformation(
                "Script-terminal pruning for root node {rootNodeTag}: skipped {Count:N0} expansion(s).",
                rootNodeLabelInLogs, _scriptTerminalPrunes);

        if (_hasActivityWindow)
            _logger.LogInformation(
                "Script activity window checks for root node {rootNodeTag}: checks={Checks:N0}, rejected={Rejected:N0}, judged on a partial scan={Partial:N0}.",
                rootNodeLabelInLogs, _activityChecks, _activityRejections, _activityPartialScans);

        TimeSpan walkElapsed;
        int walkQueries;
        lock (_stepLock)
        {
            walkElapsed = _walkWatch.Elapsed;
            walkQueries = _walkQueries;
            _walkRoot = null;
        }

        if (walkElapsed >= _slowStepThreshold)
            _logger.LogInformation(
                "Neighbor retrieval for root node {rootNodeTag} took {Elapsed:N0} s and {Queries:N0} database queries.",
                rootNodeLabelInLogs, walkElapsed.TotalSeconds, walkQueries);

        return g;
    }

    private async Task EnsureB2T(BitcoinGraph g, CancellationToken ct)
    {
        // there could be many reasons as to why a graph may not contain any Tx nodes;
        // e.g., if the graph is empty, the traversal was so small that did not include any Tx nodes. 
        if (g.NodesByType.TryGetValue(NodeKind.Tx, out var txNodes))
        {
            foreach (var txNode in txNodes)
            {
                var v = (TxNode)txNode;
                var found = false;
                if (g.EdgesByType.TryGetValue(B2TEdge.Kind, out var b2tEdges))
                {
                    foreach (var edge in b2tEdges)
                    {
                        var t = (B2TEdge)edge;
                        if (t.Target.Txid == v.Txid)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    var records = await WatchSlowStepAsync(
                        () => _graphDb.GetEdgesAsync(
                            edgeKind: B2TEdge.Kind,
                            sourceNodeVariable: "block",
                            targetNodeVariable: "tx",
                            relationshipVariable: "b2t",
                            targetNodeIdProperty: _txidMapping.Property.Name,
                            targetNodeId: _txidMapping.GetValue(v),
                            ct: ct),
                        $"block lookup for Tx {v.Txid}");

                    var record = records[0];
                    double txNodeOutHops = v.OutHopsFromRoot ?? 0;

                    TryUnpackNodeDict(
                        record["block"].As<object>().As<IDictionary<string, object>>(),
                        txNodeOutHops + 1,
                        out var builtBlockNode);

                    if (builtBlockNode != null)
                    {
                        var blockNode = g.GetOrAddNode(builtBlockNode);

                        var candidateEdge = _graphDb.StrategyFactory.CreateEdge(blockNode, v, record["b2t"].As<IRelationship>());

                        if (candidateEdge != null)
                            g.TryGetOrAddEdge(candidateEdge, out IEdge<Model.INode, Model.INode> _);
                    }
                }
            }
        }
    }

    private async Task EnsureB2BWindow(BitcoinGraph g, CancellationToken ct)
    {
        var earlier = _options.Bitcoin.GraphSample.PanoramaOptions.EarlierBlocksWindow;
        var later = _options.Bitcoin.GraphSample.PanoramaOptions.LaterBlocksWindow;

        // limit to only the blocks that were in the community before this method ran,
        // so windows do not chain outward from the newly added context blocks.
        if (!g.NodesByType.TryGetValue(NodeKind.Block, out var blockNodes))
            return;

        foreach (var n in blockNodes)
        {
            ct.ThrowIfCancellationRequested();
            var block = (BlockNode)n;

            await WalkB2BWindow(g, block, earlier, towardsEarlier: true, ct);
            await WalkB2BWindow(g, block, later, towardsEarlier: false, ct);
        }
    }

    /// <summary>
    /// In the graph db, B2B edges are stored as (h)-[:Follows]->(h+1),
    /// i.e., from the earlier block to the later block.
    /// </summary>
    private async Task WalkB2BWindow(
        BitcoinGraph g, BlockNode start, int steps, bool towardsEarlier, CancellationToken ct)
    {
        var current = start;
        double startHops = start.OutHopsFromRoot ?? 0;

        for (var i = 1; i <= steps; i++)
        {
            ct.ThrowIfCancellationRequested();

            var records = await WatchSlowStepAsync(
                () => towardsEarlier
                    ? _graphDb.GetEdgesAsync(
                        edgeKind: B2BEdge.Kind,
                        sourceNodeVariable: "earlier",
                        targetNodeVariable: "later",
                        relationshipVariable: "b2b",
                        targetNodeIdProperty: _heightMapping.Property.Name,
                        targetNodeId: current.BlockMetadata.Height,
                        ct: ct)
                    : _graphDb.GetEdgesAsync(
                        edgeKind: B2BEdge.Kind,
                        sourceNodeVariable: "earlier",
                        targetNodeVariable: "later",
                        relationshipVariable: "b2b",
                        sourceNodeIdProperty: _heightMapping.Property.Name,
                        sourceNodeId: current.BlockMetadata.Height,
                        ct: ct),
                $"{(towardsEarlier ? "earlier" : "later")} block lookup from Block {current.BlockMetadata.Height}");

            // can only happen when going backward from the first block, or forward from the last block
            if (records.Count == 0)
                break;

            var record = records[0];

            if (!TryUnpackNodeDict(
                    record[towardsEarlier ? "earlier" : "later"].As<object>().As<IDictionary<string, object>>(),
                    startHops + i,
                    out var builtNeighbor)
                || builtNeighbor is not BlockNode)
                break;

            var neighbor = (BlockNode)g.GetOrAddNode(builtNeighbor);

            var (src, dst) = towardsEarlier ? (neighbor, current) : (current, neighbor);
            var candidateEdge = _graphDb.StrategyFactory.CreateEdge(src, dst, record["b2b"].As<IRelationship>());
            if (candidateEdge != null)
                g.TryGetOrAddEdge(candidateEdge, out IEdge<Model.INode, Model.INode> _);

            current = neighbor;
        }
    }

    /// <summary>
    /// Returns up to count root nodes from the beginning of the user-provided file on each
    /// attempt when one is configured, otherwise randomly selected from the database.
    /// 
    /// TODO: an alternative return type option is to return a list of Node objects instead of tuples, but that would require
    /// some updates to the strategy factory to support creating nodes from tsv files. 
    /// Then, you couuld only return a generic list of Node objects, and the caller could use the GetIdPropertyName() method to get the property name for each node.
    /// That could simplify this method, the parser that reads list of root nodes from the tsv file, and the methods that cosume the list of root nodes.
    /// </summary>
    private async Task<List<(NodeKind Kind, string IdProperty, string Id)>> GetRootNodesAsync(int count, CancellationToken ct)
    {
        if (_options.Bitcoin.GraphSample.RootNodesFilename == null)
        {
            var randomRoots = await GetRandomScriptNodes(count, ct);
            return randomRoots.Select(root => (root.NodeKind, root.GetIdPropertyName(), root.Id)).ToList();
        }

        var roots = ReadRootNodesFile(_options.Bitcoin.GraphSample.RootNodesFilename);
        return roots.GetRange(0, Math.Min(count, roots.Count));
    }

    private List<(NodeKind Kind, string IdProperty, string Id)> ReadRootNodesFile(string filename)
    {
        var roots = new List<(NodeKind Kind, string IdProperty, string Id)>();
        var identifiers = new HashSet<(NodeKind Kind, string Id)>();
        var duplicates = 0;
        var lineNumber = 0;

        foreach (var line in File.ReadLines(filename))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var columns = line.Split('\t');
            var kindText = columns[0].Trim();
            if (lineNumber == 1 &&
                (kindText.Equals("NodeKind", StringComparison.OrdinalIgnoreCase) ||
                 kindText.Equals("Kind", StringComparison.OrdinalIgnoreCase)))
                continue;

            if (columns.Length < 2 || string.IsNullOrWhiteSpace(columns[1]))
                throw new InvalidOperationException(
                    $"Invalid root node at '{filename}', line {lineNumber}: expected a node kind and identifying property value separated by a tab.");

            if (kindText.Contains(',') || !Enum.TryParse<NodeKind>(kindText, true, out var kind) ||
                !Enum.IsDefined(kind) || kind == NodeKind.Undefined)
                throw new InvalidOperationException(
                    $"Invalid node kind '{kindText}' at '{filename}', line {lineNumber}: expected Coinbase (1), Script (2), Block (3), or Tx (4).");

            var id = columns[1].Trim();
            var idProperty = kind switch
            {
                NodeKind.Coinbase => CoinbaseNode.Kind.ToString(),
                NodeKind.Script => nameof(ScriptNode.Address),
                NodeKind.Block => nameof(BlockMetadata.Height),
                NodeKind.Tx => nameof(TxNode.Txid),
                _ => throw new InvalidOperationException($"Unsupported node kind '{kind}'.")
            };

            if (!identifiers.Add((kind, id)))
            {
                duplicates++;
                continue;
            }

            roots.Add((kind, idProperty, id));
        }

        if (roots.Count == 0)
            throw new InvalidOperationException($"No root nodes were read from '{filename}'.");

        _logger.LogInformation(
            "Read {RootCount:N0} root node(s) from {File}; skipped {DuplicateCount:N0} duplicate(s).",
            roots.Count, filename, duplicates);

        if (roots.Count < _options.Bitcoin.GraphSample.Count)
            _logger.LogWarning(
                "The root nodes file {File} contains {RootCount:N0} roots, fewer than the requested {Count:N0} graphs.",
                filename, roots.Count, _options.Bitcoin.GraphSample.Count);

        return roots;
    }

    private async Task<List<ScriptNode>> GetRandomScriptNodes(int count, CancellationToken ct)
    {
        var nodeVar = "randomNode";
        var rndRecords = await WatchSlowStepAsync(
            () => _graphDb.GetRandomNodesAsync(
                ScriptNode.Kind,
                count,
                ct,
                _options.Bitcoin.GraphSample.RootNodeSelectProb,
                nodeVar),
            $"random selection of {count:N0} root Script(s)");

        var rndNodes = new List<ScriptNode>();
        foreach (var n in rndRecords)
        {
            if (_graphDb.StrategyFactory.TryCreateNode<ScriptNode>(
                n.Values[nodeVar].As<Neo4j.Driver.INode>(),
                out var node, 0, 0, 0))
            {
                rndNodes.Add(node);
            }
        }

        return rndNodes;
    }

    /// <summary>
    /// Returns true if *every* block height recorded on the given relationship lies
    /// inside [minEdgeActivityHeight, maxEdgeActivityHeight] (inclusive). Which
    /// properties hold heights depends on the relationship type (see the edge descriptors):
    ///   Redeems (Script->Tx): CreationHeight and SpentHeight
    ///   Credits (Tx->Script): CreationHeight and SpentHeight
    ///                         (SpentHeight == long.MaxValue means still unspent and is skipped)
    ///   everything else:      Height
    /// An edge with any height outside the window is excluded. Relationships missing
    /// an expected height property (e.g. Follows) are treated as outside the window.
    /// </summary>
    private bool IsWithinEdgeActivityWindow(IRelationship edge)
    {
        if (!_hasEdgeActivityWindow)
            return true;

        string[] propertyNames = _defaultHeightProperties;
        if (Enum.TryParse<RelationType>(edge.Type, ignoreCase: true, out var relation))
        {
            propertyNames = relation switch
            {
                RelationType.Redeems => _redeemsHeightProperties,
                RelationType.Credits => _creditsHeightProperties,
                _ => _defaultHeightProperties,
            };
        }

        foreach (var propertyName in propertyNames)
        {
            if (!edge.Properties.TryGetValue(propertyName, out var raw) || raw is null)
                return false;

            long height;
            try
            {
                height = Convert.ToInt64(raw);
            }
            catch (Exception e) when (e is FormatException || e is InvalidCastException || e is OverflowException)
            {
                return false;
            }

            if (height == _unspentSentinel)
                continue;

            if (height < _minEdgeActivityHeight || height > _maxEdgeActivityHeight)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Activity-window check for a candidate. Non-Script nodes always pass: by design Tx and
    /// Block nodes are tied to a single block height. For a Script, runs one aggregate query
    /// that returns its in/out edge totals and, per configured window, how many of those
    /// edges have their activity height inside the window; then applies the percentage
    /// thresholds. Results are memoized per sampled graph.
    /// </summary>
    private async Task<bool> CheckScriptActivityAsync(Model.INode node, CancellationToken ct)
    {
        if (node.NodeKind != NodeKind.Script)
            return true;

        if (_activityCheckCache.TryGetValue(node.Id, out var cache))
            return cache;

        // The scan is bounded by activityScanLimit so a hub (an exchange or mixer address
        // with millions of edges) cannot stall sampling; for such nodes the percentage is
        // estimated over the scanned edges (see NodeActivityCounts).
        var counts = await WatchSlowStepAsync(
            () => _graphDb.GetNodeActivityCountsAsync(
                node.NodeKind,
                node.GetIdPropertyName(),
                node.Id,
                _activityRanges,
                _options.Bitcoin.GraphSample.PanoramaOptions.ActivityScanLimit,
                ct),
            $"activity count query for Script {node.Id} (in={node.OriginalInDegree ?? 0:N0}, out={node.OriginalOutDegree ?? 0:N0})");

        if (!counts.IsComplete)
        {
            _activityPartialScans++;
            _logger.LogDebug(
                "Script {Id} activity judged on a partial scan: scanned {Scanned:N0} of {Total:N0} edges.",
                node.Id, counts.InScanned + counts.OutScanned, counts.InTotal + counts.OutTotal);
        }

        var ok =
            SatisfiesActivityWindow(_incomingActivityWindow, _incomingRangeIndex, counts, incoming: true, outgoing: false) &&
            SatisfiesActivityWindow(_outgoingActivityWindow, _outgoingRangeIndex, counts, incoming: false, outgoing: true) &&
            SatisfiesActivityWindow(_totalActivityWindow, _totalRangeIndex, counts, incoming: true, outgoing: true);

        _activityChecks++;
        if (!ok)
            _activityRejections++;

        _activityCheckCache[node.Id] = ok;
        return ok;
    }

    private static bool SatisfiesActivityWindow(
        ActivityWindowOptions? w,
        int rangeIndex,
        NodeActivityCounts counts,
        bool incoming,
        bool outgoing)
    {
        if (w == null)
            return true;

        // Minimum activity is judged on the exact degree.
        long activityCount = (incoming ? counts.InTotal : 0) + (outgoing ? counts.OutTotal : 0);

        if (activityCount < w.MinActivityCount || activityCount <= 0)
            return false;

        // The percentage is judged on the scanned edges. When the scan was complete the
        // scanned counts equal the degrees, so this is exact; otherwise it is an estimate
        // over the scanned sample.
        long scanned = (incoming ? counts.InScanned : 0) + (outgoing ? counts.OutScanned : 0);

        // Partial scan that happened to cover no edge in the considered direction:
        // nothing to estimate from.
        if (scanned <= 0)
            return false;

        var (inRange, outRange) = counts.InRange[rangeIndex];
        double numerator = (incoming ? inRange : 0) + (outgoing ? outRange : 0);

        return (numerator * 100.0 / scanned) >= w.MinPercentage;
    }

    /// <summary>
    /// Runs a database step while marking it as the step in progress. Logs nothing while
    /// it is fast. The timer started in SampleAsync reports the marked step once it has
    /// run longer than the slow-step threshold (see ReportSlowStep), and this method logs
    /// the step's total duration when it ends if it was reported. The step is invoked
    /// inside this method so that any synchronous blocking before its first await is
    /// covered as well.
    /// </summary>
    private async Task<T> WatchSlowStepAsync<T>(Func<Task<T>> step, string description)
    {
        lock (_stepLock)
        {
            _currentStep = description;
            _currentStepLastReport = TimeSpan.Zero;
            _currentStepWatch.Restart();
            _walkQueries++;
        }

        try
        {
            return await step();
        }
        finally
        {
            TimeSpan elapsed;
            bool reported;
            lock (_stepLock)
            {
                elapsed = _currentStepWatch.Elapsed;
                reported = _currentStepLastReport > TimeSpan.Zero;
                _currentStep = null;
            }

            if (reported)
                _logger.LogWarning(
                    "Slow step finished: {Step} took {Elapsed:N1} s.",
                    description, elapsed.TotalSeconds);
        }
    }

    /// <summary>
    /// Timer callback: warns about the step in progress if it is slow, and reports the
    /// progress of a long-running neighborhood walk once per interval.
    /// </summary>
    private void ReportSlowStep()
    {
        string? step = null;
        TimeSpan stepElapsed = TimeSpan.Zero;

        string? walkRoot = null;
        TimeSpan walkElapsed = TimeSpan.Zero;
        int walkQueries = 0, walkHop = 0, walkQueue = 0, walkNodes = 0, walkEdges = 0;
        int activityChecks = 0, activityRejections = 0;

        lock (_stepLock)
        {
            if (_currentStep != null)
            {
                stepElapsed = _currentStepWatch.Elapsed;
                var due = _currentStepLastReport == TimeSpan.Zero
                    ? stepElapsed >= _slowStepThreshold
                    : stepElapsed - _currentStepLastReport >= _slowStepRepeat;

                if (due)
                {
                    step = _currentStep;
                    _currentStepLastReport = stepElapsed;
                }
            }

            if (_walkRoot != null)
            {
                walkElapsed = _walkWatch.Elapsed;
                if (walkElapsed - _walkLastReport >= _walkProgressInterval)
                {
                    walkRoot = _walkRoot;
                    _walkLastReport = walkElapsed;
                    walkQueries = _walkQueries;
                    walkHop = _walkHop;
                    walkQueue = _walkQueue;
                    walkNodes = _walkNodes;
                    walkEdges = _walkEdges;
                    activityChecks = _activityChecks;
                    activityRejections = _activityRejections;
                }
            }
        }

        if (step != null)
            _logger.LogWarning(
                "Slow step: {Step} has been running for {Elapsed:N0} s.",
                step, stepElapsed.TotalSeconds);

        if (walkRoot != null)
            _logger.LogInformation(
                "Still expanding root node {rootNodeTag} after {Elapsed:N0} s: hop {Hop}, {Queue:N0} node(s) queued, " +
                "graph has {Nodes:N0} nodes and {Edges:N0} edges, {Queries:N0} database queries so far, " +
                "activity checks={Checks:N0} (rejected={Rejected:N0}).",
                walkRoot, walkElapsed.TotalSeconds, walkHop, walkQueue, walkNodes, walkEdges,
                walkQueries, activityChecks, activityRejections);
    }

    private static string Describe(ActivityWindowOptions? w)
    {
        return w == null
            ? "off"
            : $"[{w.MinHeight:N0}, {w.MaxHeight:N0}] >= {w.MinPercentage}% of >= {w.MinActivityCount:N0} activities";
    }

    private bool TryUnpackNodeDict(IDictionary<string, object> dict, double hop, out Model.INode? v)
    {
        v = null;
        var node = dict["node"].As<Neo4j.Driver.INode>();
        var inDegree = Convert.ToDouble(dict["inDegree"]);
        var outDegree = Convert.ToDouble(dict["outDegree"]);
        if (node is null)
            return false;
        return
            _graphDb.StrategyFactory.TryCreateNode(
                node: node,
                out v,
                originalIndegree: inDegree,
                originalOutdegree: outDegree,
                outHopsFromRoot: hop);
    }
}
