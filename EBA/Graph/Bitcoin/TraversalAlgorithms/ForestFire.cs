using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

namespace EBA.Graph.Bitcoin.TraversalAlgorithms;

public class ForestFire : ITraversalAlgorithm
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<BitcoinGraphAgent> _logger;

    private int _maxHopReached = 0;

    public ForestFire(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphAgent> logger)
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
                var rootNodeLabelInLogs = $"({NodeLabels.Script} {{{rootNode.GetIdPropertyName()}={rootNode.Id}}})";

                _logger.LogInformation(
                    "Sampling neighbors for root {Index:N0}/{Total:N0}. {rootNodeTag}",
                    ++counter, rndRootNodes.Count, rootNodeLabelInLogs);

                var graph = await GetNeighborsAsync(
                    rootNodeLabel: NodeLabels.Script,
                    rootNodeIdProperty: rootNode.GetIdPropertyName(),
                    rootNodeId: rootNode.Id,
                    nodeSamplingCountAtRoot: _options.Bitcoin.GraphSample.ForestFireOptions.NodeSamplingCountAtRoot,
                    maxHops: _options.Bitcoin.GraphSample.ForestFireOptions.MaxHops,
                    queryLimit: _options.Bitcoin.GraphSample.ForestFireOptions.QueryLimit,
                    nodeCountReductionFactorByHop: _options.Bitcoin.GraphSample.ForestFireOptions.NodeCountReductionFactorByHop,
                    ct: ct);

                var perBatchLabelsFilename = Path.Join(_options.WorkingDir, "Labels.tsv");

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
                    graph.Serialize(
                        graphDir,
                        perBatchLabelsFilename,
                        serializeFeatureVectors: _options.Bitcoin.GraphSample.SerializeFeatureVectors,
                        serializeEdges: _options.Bitcoin.GraphSample.SerializeEdges);

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
    private List<Model.INode> ProcessQueriedNeighborhood(
        List<IRecord> samplingResult, 
        int hop,
        int nodeSamplingCountAtRoot, 
        double nodeCountReductionFactorByHop, 
        BitcoinGraph g,
        CancellationToken ct)
    {
        static bool TryUnpackDict(IDictionary<string, object> dict, double hop, out Model.INode? v)
        {
            v = null;
            var node = dict["node"].As<Neo4j.Driver.INode>();
            var inDegree = Convert.ToDouble(dict["inDegree"]);
            var outDegree = Convert.ToDouble(dict["outDegree"]);
            if (node is null) 
                return false;
            return NodeFactory.TryCreateNode(node, inDegree, outDegree, hop, out v);
        }

        var nodesAddedToGraph = new List<Model.INode>();
        if (samplingResult.Count == 0)
            return nodesAddedToGraph;

        var nodesUniqueToThisHop = new Dictionary<string, Model.INode>();
        var nodesInThisHopAlreadyInGraph = new Dictionary<string, Model.INode>();
        var edges = new List<IRelationship>();

        var rootList = samplingResult[0]["root"].As<List<object>>();
        if (!TryUnpackDict(rootList[0].As<IDictionary<string, object>>(), hop, out var builtRootNode) || builtRootNode == null)
            return nodesAddedToGraph;

        Model.INode rootNode = g.GetOrAddNode(builtRootNode);

        if (hop == 0)
            g.AddLabel("RootNodeId", rootNode.Id);

        for (int i = 1; i < samplingResult.Count; i++)
        {
            var r = samplingResult[i];
            foreach (var nodeObject in r["nodes"].As<List<object>>())
            {
                if (!TryUnpackDict(nodeObject.As<IDictionary<string, object>>(), hop, out var node)
                    || node == null
                    || node.IdInGraphDb == null)
                    continue;

                if (g.TryGetNode(node.Id, out var nodeInG))
                    nodesInThisHopAlreadyInGraph.TryAdd(node.IdInGraphDb, nodeInG);
                else
                    nodesUniqueToThisHop.TryAdd(node.IdInGraphDb, node);
            }

            foreach (var edge in r.Values["relationships"].As<List<IRelationship>>())
                edges.Add(edge);
        }

        ct.ThrowIfCancellationRequested();

        var rnd = new Random(31);
        var nodesToKeep = nodesUniqueToThisHop.Keys.OrderBy(
            x => rnd.Next()).Take(
                (int)Math.Floor(nodeSamplingCountAtRoot - hop * nodeCountReductionFactorByHop))
            .ToHashSet();

        foreach (var edge in edges)
        {
            if (g.NodeCount >= _options.Bitcoin.GraphSample.MaxNodeCount
                || g.EdgeCount >= _options.Bitcoin.GraphSample.MaxEdgeCount)
            {
                // Reached maximum node or edge count; stopping further additions.
                break;
            }

            string subjectNodeGraphDbId;
            if (edge.StartNodeElementId == rootNode.IdInGraphDb)
                subjectNodeGraphDbId = edge.EndNodeElementId;
            else if (edge.EndNodeElementId == rootNode.IdInGraphDb)
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
            
            if (edge.StartNodeElementId == rootNode.IdInGraphDb)
                g.GetOrAddEdge(edge, rootNode, subjectNode);
            else
                g.GetOrAddEdge(edge, subjectNode, rootNode);
        }

        return nodesAddedToGraph;
    }

    private async Task<bool> ProcessHops(
        NodeLabels rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int maxHops,
        int hop,
        int queryLimit,
        int nodeSamplingCountAtRoot,
        double nodeCountReductionFactorByHop,
        BitcoinGraph g,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var samplingResult = await _graphDb.GetNeighborsAsync(
            rootNodeLabel,
            rootNodeIdProperty,
            rootNodeId,
            queryLimit,
            1,
            true,
            ct: ct);

        var selectedNodes = ProcessQueriedNeighborhood(
            samplingResult, hop,
            nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
            nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
            g: g,
            ct: ct);

        if (hop < maxHops)
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

                await ProcessHops(
                    rootNodeLabel: BitcoinGraphAgent.ConvertGraphComponentTypeToNodeLabel(node.GetGraphComponentType()),
                    rootNodeIdProperty: node.GetIdPropertyName(),
                    rootNodeId: node.Id,
                    hop: hop + 1,
                    maxHops: maxHops,
                    queryLimit: queryLimit,
                    nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
                    nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
                    g: g,
                    ct: ct);
            }
        }

        return true;
    }

    private async Task<GraphBase> GetNeighborsAsync(
        NodeLabels rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int nodeSamplingCountAtRoot,
        int maxHops,
        int queryLimit,
        double nodeCountReductionFactorByHop,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var g = new BitcoinGraph();

        var rootNodeLabelInLogs = $"({rootNodeLabel} {{{rootNodeIdProperty}={rootNodeId}}})";
        _logger.LogInformation(
            "Retrieving neighbors for root node {rootNodeTag} up to {MaxHops} hop(s). ",
            rootNodeLabelInLogs,
            maxHops);

        _maxHopReached = 0;
        var completedWalk = await ProcessHops(
            rootNodeLabel: rootNodeLabel,
            rootNodeIdProperty: rootNodeIdProperty,
            rootNodeId: rootNodeId,
            maxHops: maxHops,
            hop: 0,
            queryLimit: queryLimit,
            nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
            nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
            g: g,
            ct: ct);

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

    private async Task<List<ScriptNode>> GetRandomScriptNodes(int count, CancellationToken ct)
    {
        var nodeVar = "randomNode";
        var rndRecords = await _graphDb.GetRandomNodesAsync(
            NodeLabels.Script,
            count,
            ct,
            _options.Bitcoin.GraphSample.RootNodeSelectProb,
            nodeVar);

        var rndNodes = new List<ScriptNode>();
        foreach (var n in rndRecords)
            rndNodes.Add(ScriptNodeStrategy.GetNodeFromProps(n.Values[nodeVar].As<Neo4j.Driver.INode>(), 0, 0, 0));

        return rndNodes;
    }
}