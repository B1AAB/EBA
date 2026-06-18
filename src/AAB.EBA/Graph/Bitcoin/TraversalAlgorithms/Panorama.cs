using AAB.EBA.Graph.Bitcoin.Descriptors;

namespace AAB.EBA.Graph.Bitcoin.TraversalAlgorithms;

public class Panorama : ITraversalAlgorithm
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<BitcoinGraphOrchestrator> _logger;

    private static readonly PropertyMapping<TxNode> _txidMapping = TxNodeDescriptor.StaticMapper.GetMapping(x => x.Txid);

    private int _maxHopReached = 0;

    public Panorama(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphOrchestrator> logger)
    {
        _options = options;
        _graphDb = graphDb;
        _logger = logger;
    }

    public async Task SampleAsync(CancellationToken ct)
    {
        var sampledSubGraphsCount = 0;
        var attempts = 0;

        _logger.LogInformation("Sampling {n:N0} graphs.", _options.Bitcoin.GraphSample.Count - sampledSubGraphsCount);

        while (
            sampledSubGraphsCount < _options.Bitcoin.GraphSample.Count &&
            ++attempts <= _options.Bitcoin.GraphSample.MaxAttempts)
        {
            var remaining = _options.Bitcoin.GraphSample.Count - sampledSubGraphsCount;
            _logger.LogInformation(
                "Attempt {Attempt}/{MaxAttempts}: querying {remaining:N0} random root node(s) for sampling.",
                attempts, _options.Bitcoin.GraphSample.MaxAttempts, remaining);

            var rndRootNodes = await GetRandomScriptNodes(remaining, ct);

            _logger.LogInformation(
                "Attempt {Attempt}: Selected {r:N0} random root node(s).",
                attempts, rndRootNodes.Count);

            int counter = 0;
            foreach (var rootNode in rndRootNodes)
            {
                ct.ThrowIfCancellationRequested();
                var rootNodeLabelInLogs = $"({ScriptNode.Kind} {{{rootNode.GetIdPropertyName()}={rootNode.Id}}})";

                _logger.LogInformation(
                    "Sampling neighbors for root {Index:N0}/{Total:N0}. {rootNodeTag}",
                    ++counter, rndRootNodes.Count, rootNodeLabelInLogs);

                var graph = await GetNeighborsAsync(
                    rootNodeLabel: ScriptNode.Kind,
                    rootNodeIdProperty: rootNode.GetIdPropertyName(),
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
    private List<Model.INode> ProcessNeighborhood(
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

        Model.INode parentNode = g.GetOrAddNode(builtParentNode);

        if (hop == 0)
            g.AddLabel("RootNodeId", parentNode.Id);

        var coinbaseNodeIdInGraphDb = "";
        for (var i = 1; i < samplingResult.Count; i++)
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
                edges.Add(edge);
        }

        ct.ThrowIfCancellationRequested();

        var rnd = new Random(31);

        var parentNodeDegree = (parentNode.OriginalInDegree ?? 0) + (parentNode.OriginalOutDegree ?? 0);
        var samplingPercentage =
            _options.Bitcoin.GraphSample.PanoramaOptions.NodeSamplingPercentageAtRoot -
            (hop * _options.Bitcoin.GraphSample.PanoramaOptions.NeighborhoodSamplePercentagePerHop);
        var neighborsToKeep = (int)Math.Floor((parentNodeDegree * samplingPercentage) / 100.0);
        var count = Math.Min(
            _options.Bitcoin.GraphSample.PanoramaOptions.MaxNeighborsPerNode,
            neighborsToKeep - nodesInThisHopAlreadyInGraph.Count);

        var nodesToKeep = nodesUniqueToThisHop.Keys.OrderBy(x => rnd.Next()).Take(count).ToHashSet();

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

            var rawNeighborhood = await _graphDb.GetNeighborhoodAsync(
                currentNode.Label,
                currentNode.IdProperty,
                currentNode.Id,
                _options.Bitcoin.GraphSample.PanoramaOptions.QueryLimit,
                1,
                true,
                ct: ct);

            var nodesAddedToGraph = ProcessNeighborhood(
                rawNeighborhood,
                currentNode.Hop,
                g: g,
                ct: ct);

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

    private async Task<bool> ProcessHopsDFS(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int hop,
        BitcoinGraph g,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var samplingResult = await _graphDb.GetNeighborhoodAsync(
            rootNodeLabel,
            rootNodeIdProperty,
            rootNodeId,
            _options.Bitcoin.GraphSample.PanoramaOptions.QueryLimit,
            1,
            true,
            ct: ct);

        var selectedNodes = ProcessNeighborhood(
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
                    var records = await _graphDb.GetEdgesAsync(
                        edgeKind: B2TEdge.Kind,
                        sourceNodeVariable: "block",
                        targetNodeVariable: "tx",
                        relationshipVariable: "b2t",
                        targetNodeIdProperty: _txidMapping.Property.Name,
                        targetNodeId: _txidMapping.GetValue(v),
                        ct: ct);

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

    private async Task<List<ScriptNode>> GetRandomScriptNodes(int count, CancellationToken ct)
    {
        var nodeVar = "randomNode";
        var rndRecords = await _graphDb.GetRandomNodesAsync(
            ScriptNode.Kind,
            count,
            ct,
            _options.Bitcoin.GraphSample.RootNodeSelectProb,
            nodeVar);

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
