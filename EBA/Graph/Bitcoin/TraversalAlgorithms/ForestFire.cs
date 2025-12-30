using EBA.Utilities;

namespace EBA.Graph.Bitcoin.TraversalAlgorithms;

public class ForestFire : ITraversalAlgorithm
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<BitcoinGraphAgent> _logger;

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
        var baseOutputDir = Path.Join(_options.WorkingDir, $"sampled_subgraphs_{Helpers.GetUnixTimeSeconds()}");

        _logger.LogInformation("Sampling {n} graphs.", _options.GraphSample.Count - sampledSubGraphsCount);

        while (
            sampledSubGraphsCount < _options.GraphSample.Count &&
            ++attempts <= _options.GraphSample.MaxAttempts)
        {
            _logger.LogInformation(
                "Getting {n} random root nodes; attempt {a}/{m}.",
                _options.GraphSample.Count - sampledSubGraphsCount,
                attempts, _options.GraphSample.MaxAttempts);

            var rndRootNodes = await GetRandomScriptNodes(_options.GraphSample.Count - sampledSubGraphsCount, ct);

            _logger.LogInformation("Selected {n} random root nodes.", rndRootNodes.Count);

            int counter = 1;
            foreach (var rootNode in rndRootNodes)
            {
                _logger.LogInformation("Sampling neighbors of the random root node {n}/{t}.", counter, rndRootNodes.Count);

                var graph = await GetNeighborsAsync(
                    rootNodeLabel: NodeLabels.Script,
                    rootNodeIdProperty: rootNode.GetIdPropertyName(),
                    rootNodeId: rootNode.Id,
                    nodeSamplingCountAtRoot: _options.GraphSample.ForestFireNodeSamplingCountAtRoot,
                    maxHops: _options.GraphSample.ForestFireMaxHops,
                    queryLimit: _options.GraphSample.ForestFireQueryLimit,
                    nodeCountReductionFactorByHop: _options.GraphSample.ForestFireNodeCountReductionFactorByHop);
                var perBatchLabelsFilename = Path.Join(_options.WorkingDir, "Labels.tsv");

                if (graph.NodeCount < _options.GraphSample.MinNodeCount - (_options.GraphSample.MinNodeCount * 0.0) ||
                    graph.EdgeCount < _options.GraphSample.MinEdgeCount - (_options.GraphSample.MinEdgeCount * 0.0))
                {
                    _logger.LogWarning(
                        "The sampled graph with {a} nodes and {b} edges does not match required charactersitics: " +
                        "MinNodeCount: {c}, MaxNodeCount: {d}, MinEdgeCount: {e}, MaxEdgeCount: {f}",
                        graph.NodeCount,
                        graph.EdgeCount,
                        _options.GraphSample.MinNodeCount,
                        _options.GraphSample.MaxNodeCount,
                        _options.GraphSample.MinEdgeCount,
                        _options.GraphSample.MaxEdgeCount);

                    _logger.LogWarning(
                        "Failed sampling neighbors of the root node {r}.",
                        rootNode.Address);
                }
                else
                {
                    graph.Serialize(
                        Path.Join(_options.WorkingDir, graph.Id),
                        perBatchLabelsFilename,
                        serializeFeatureVectors: _options.GraphSample.SerializeFeatureVectors,
                        serializeEdges: _options.GraphSample.SerializeEdges);

                    _logger.LogInformation("Serialized the graph.");

                    sampledSubGraphsCount++;
                    counter++;
                }
            }
        }
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
        BitcoinGraph g)
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
                if (!TryUnpackDict(nodeObject.As<IDictionary<string, object>>(), hop, out var node) || node == null || node.IdInGraphDb == null)
                    continue;

                if (g.TryGetNode(node.Id, out var nodeInG))
                    nodesInThisHopAlreadyInGraph.TryAdd(node.IdInGraphDb, nodeInG);
                else
                    nodesUniqueToThisHop.TryAdd(node.IdInGraphDb, node);
            }

            foreach (var edge in r.Values["relationships"].As<List<IRelationship>>())
                edges.Add(edge);
        }

        var rnd = new Random(31);
        var nodesToKeep = nodesUniqueToThisHop.Keys.OrderBy(
            x => rnd.Next()).Take(
                (int)Math.Floor(nodeSamplingCountAtRoot - hop * nodeCountReductionFactorByHop))
            .ToHashSet();

        foreach (var edge in edges)
        {
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

    private async Task ProcessHops(
        NodeLabels rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int maxHops,
        int hop,
        int queryLimit,
        int nodeSamplingCountAtRoot,
        double nodeCountReductionFactorByHop,
        BitcoinGraph g)
    {
        var samplingResult = await _graphDb.GetNeighborsAsync(
            rootNodeLabel,
            rootNodeIdProperty,
            rootNodeId,
            queryLimit,
            1,
            GraphTraversal.BFS);

        var selectedNodes = ProcessQueriedNeighborhood(
            samplingResult, hop,
            nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
            nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
            g: g);

        if (hop < maxHops)
        {
            foreach (var node in selectedNodes)
                await ProcessHops(
                    rootNodeLabel: BitcoinGraphAgent.ConvertGraphComponentTypeToNodeLabel(node.GetGraphComponentType()),
                    rootNodeIdProperty: node.GetIdPropertyName(),
                    rootNodeId: node.Id,
                    hop: hop + 1,
                    maxHops: maxHops,
                    queryLimit: queryLimit,
                    nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
                    nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
                    g: g);
        }
    }

    private async Task<GraphBase> GetNeighborsAsync(
        NodeLabels rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int nodeSamplingCountAtRoot,
        int maxHops,
        int queryLimit,
        double nodeCountReductionFactorByHop)
    {
        var g = new BitcoinGraph();

        _logger.LogInformation(
            "Getting neighbors of random node ({label} {{{property}: {value}}}) at {hop} hop distance.",
            rootNodeLabel.ToString(),
            rootNodeIdProperty,
            rootNodeId,
            maxHops.ToString());

        // temp
        rootNodeId = "15PSwPAeSB9opMRigpSrJPatGdfKBV4LxY";

        await ProcessHops(
            rootNodeLabel: rootNodeLabel,
            rootNodeIdProperty: rootNodeIdProperty,
            rootNodeId: rootNodeId,
            maxHops: maxHops,
            hop: 0,
            queryLimit: queryLimit,
            nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
            nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
            g: g);

        _logger.LogInformation("Retrieved neighbors.");
        _logger.LogInformation("Building a graph from the neighbors.");

        _logger.LogInformation("Build graph from the neighbors; {nodeCount} nodes and {edgeCount} edges.", g.NodeCount, g.EdgeCount);

        return g;
    }

    private async Task<List<ScriptNode>> GetRandomScriptNodes(int count, CancellationToken ct)
    {
        var nodeVar = "randomNode";
        var rndRecords = await _graphDb.GetRandomNodesAsync(
            NodeLabels.Script,
            count,
            ct,
            _options.GraphSample.RootNodeSelectProb,
            nodeVar);

        var rndNodes = new List<ScriptNode>();
        foreach (var n in rndRecords)
            rndNodes.Add(new ScriptNode(n.Values[nodeVar].As<Neo4j.Driver.INode>()));

        return rndNodes;
    }
}