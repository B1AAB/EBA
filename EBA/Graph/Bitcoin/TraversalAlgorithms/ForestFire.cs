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
                    rootScriptAddress: rootNode.Address,
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

    private List<Model.INode> ProcessSamplingResult(
        List<IRecord> samplingResult, 
        int hop, 
        string rootScriptAddress, 
        HashSet<string> allNodesAddedToGraph, 
        HashSet<string> allEdgesAddedToGraph, 
        int nodeSamplingCountAtRoot, 
        double nodeCountReductionFactorByHop, 
        BitcoinGraph g)
    {
        static (Neo4j.Driver.INode, double, double, double) UnpackDict(IDictionary<string, object> dict, double hop)
        {
            var node = dict["node"].As<Neo4j.Driver.INode>();
            var inDegree = Convert.ToDouble(dict["inDegree"]);
            var outDegree = Convert.ToDouble(dict["outDegree"]);
            return (node, inDegree, outDegree, hop);
        }

        var rnd = new Random(31);

        Node root;
        var nodes = new Dictionary<string, (Neo4j.Driver.INode, double, double, double)>();
        var edges = new Dictionary<string, IRelationship>();

        var rootNodeId = "";

        // TODO: this iteration needs to be improved, maybe I have a list like this because of the query?!
        foreach (var r in samplingResult)
        {
            if (rootScriptAddress == BitcoinChainAgent.Coinbase.ToString())
            {
                var rootList = r["root"].As<List<object>>();
                (Neo4j.Driver.INode rootNode, double inDegree, double outDegree, double hopsFromRoot) = UnpackDict(rootList[0].As<IDictionary<string, object>>(), hop);

                if (rootNode is null)
                    continue;

                root = new CoinbaseNode(rootNode, originalOutdegree: outDegree, hopsFromRoot: hopsFromRoot);

                if (!allNodesAddedToGraph.Contains(root.Id))
                {
                    g.GetOrAddNode(GraphComponentType.BitcoinCoinbaseNode, root);
                    allNodesAddedToGraph.Add(root.Id);
                }

                rootNodeId = root.Id;
            }
            else
            {
                var rootList = r["root"].As<List<object>>();
                (Neo4j.Driver.INode rootB, double inDegree, double outDegree, double outHopsFromRoot) = UnpackDict(rootList[0].As<IDictionary<string, object>>(), hop);

                if (rootB is null)
                    continue;

                root = new ScriptNode(rootB, originalIndegree: inDegree, originalOutdegree: outDegree, outHopsFromRoot: outHopsFromRoot);

                if (!allNodesAddedToGraph.Contains(root.Id))
                {
                    g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, root);
                    allNodesAddedToGraph.Add(root.Id);
                }

                rootNodeId = root.Id;
            }

            foreach (var nodeObject in r["nodes"].As<List<object>>())
            {
                (Neo4j.Driver.INode node, double inDegree, double outDegree, double hopsFromRoot) = UnpackDict(nodeObject.As<IDictionary<string, object>>(), hop);
                if (!allNodesAddedToGraph.Contains(node.ElementId))
                    nodes.TryAdd(node.ElementId, (node, inDegree, outDegree, hopsFromRoot));
            }

            foreach (var edge in r.Values["relationships"].As<List<IRelationship>>())
                if (!allEdgesAddedToGraph.Contains(edge.ElementId))
                    edges.TryAdd(edge.ElementId, edge);
        }

        if (hop == 0)
            g.AddLabel("RootNodeId", rootNodeId);

        var nodesToKeep = nodes.Keys.OrderBy(x => rnd.Next()).Take((int)Math.Floor(nodeSamplingCountAtRoot - hop * nodeCountReductionFactorByHop)).ToList();
        var nodesToKeepIds = new HashSet<string>();
        foreach (var nodeId in nodesToKeep)
            if (!allNodesAddedToGraph.Contains(nodeId))
                nodesToKeepIds.Add(nodeId);

        var addedNodes = new List<Model.INode>();

        foreach (var edge in edges)
        {
            var targetNodeId = edge.Value.EndNodeElementId;
            if (nodesToKeepIds.Contains(targetNodeId))
            {
                // so only the "connected" nodes are added.
                // also, this order is important where 1st the node is added, then the edge.
                (var ccNode, var indegree, var outdegree, var outHopsFromRoot) = nodes[targetNodeId];
                addedNodes.Add(g.GetOrAddNode(BitcoinGraph.NodeFactory(ccNode, originalIndegree: indegree, originalOutdegree: outdegree, outHopsFromRoot: outHopsFromRoot)));
                allNodesAddedToGraph.Add(targetNodeId);

                g.GetOrAddEdge(edge.Value);
                allEdgesAddedToGraph.Add(edge.Value.ElementId);
            }
        }

        return addedNodes;
    }

    private async Task ProcessHops(
        NodeLabels rootNodeLabel,
        string propKey,
        string propValue,
        int maxHops,
        int hop,
        string rootScriptAddress,
        int queryLimit,
        HashSet<string> allNodesAddedToGraph,
        HashSet<string> allEdgesAddedToGraph,
        int nodeSamplingCountAtRoot,
        double nodeCountReductionFactorByHop,
        BitcoinGraph g)
    {
        var samplingResult = await _graphDb.GetNeighborsAsync(rootNodeLabel, propKey, propValue, queryLimit, 1, GraphTraversal.BFS);

        var selectedNodes = ProcessSamplingResult(
            samplingResult, hop,
            rootScriptAddress: rootScriptAddress,
            allNodesAddedToGraph: allNodesAddedToGraph,
            allEdgesAddedToGraph: allEdgesAddedToGraph,
            nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
            nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
            g: g);

        if (hop < maxHops)
        {
            foreach (var node in selectedNodes)
                if (node.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode) // TODO: this is currently a limitation since we currently do not support root nodes of other types.
                    await ProcessHops(
                        rootNodeLabel: rootNodeLabel,
                        propKey: "Address",
                        propValue: ((ScriptNode)node).Address,
                        hop: hop + 1,
                        maxHops: maxHops,
                        rootScriptAddress: rootScriptAddress,
                        queryLimit: queryLimit,
                        allNodesAddedToGraph: allNodesAddedToGraph,
                        allEdgesAddedToGraph: allEdgesAddedToGraph,
                        nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
                        nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
                        g: g);
        }
    }

    private async Task<GraphBase> GetNeighborsAsync(
        NodeLabels rootNodeLabel,
        string rootScriptAddress,
        int nodeSamplingCountAtRoot,
        int maxHops,
        int queryLimit,
        double nodeCountReductionFactorByHop)
    {
        var g = new BitcoinGraph();
        var allNodesAddedToGraph = new HashSet<string>();
        var allEdgesAddedToGraph = new HashSet<string>();

        _logger.LogInformation("Getting neighbors of random node {node}, at {hop} hop distance.", rootScriptAddress, maxHops);

        await ProcessHops(
            rootNodeLabel: rootNodeLabel,
            propKey: "Address",
            propValue: rootScriptAddress,
            maxHops: maxHops,
            hop: 0,
            rootScriptAddress: rootScriptAddress,
            queryLimit: queryLimit,
            allNodesAddedToGraph: allNodesAddedToGraph,
            allEdgesAddedToGraph: allEdgesAddedToGraph,
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