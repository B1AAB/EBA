using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;
using EBA.Utilities;

using Neo4j.Driver;

namespace EBA.Graph.Bitcoin.Samplers;

public class ForestFire
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<GraphAgent> _logger;

    public ForestFire(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<GraphAgent> logger)
    {
        _options = options;
        _graphDb = graphDb;
        _logger = logger;
    }

    public async Task SampleAsync(CancellationToken ct)
    {
        var sampledGraphsCounter = 0;
        var attempts = 0;
        var baseOutputDir = Path.Join(_options.WorkingDir, $"sampled_graphs_{Helpers.GetUnixTimeSeconds()}");

        _logger.LogInformation("Sampling {n} graphs.", _options.GraphSample.Count - sampledGraphsCounter);

        while (
            sampledGraphsCounter < _options.GraphSample.Count &&
            ++attempts <= _options.GraphSample.MaxAttempts)
        {
            _logger.LogInformation(
                "Getting {n} random root nodes; attempt {a}/{m}.",
                _options.GraphSample.Count - sampledGraphsCounter,
                attempts, _options.GraphSample.MaxAttempts);

            var rndRootNodes = await _graphDb.GetRandomNodes(
                ScriptNodeStrategy.Labels,
                _options.GraphSample.Count - sampledGraphsCounter,
                _options.GraphSample.RootNodeSelectProb);

            _logger.LogInformation("Selected {n} random root nodes.", rndRootNodes.Count);

            int counter = 0;
            foreach (var rootNode in rndRootNodes)
            {
                _logger.LogInformation("Sampling neighbors of the random root node {n}/{t}.", ++counter, rndRootNodes.Count);

                IDriver driver = null; // TODO TEMP

                var graph = await GetNeighbors(
                    rootScriptAddress: ((ScriptNode)rootNode).Address, // todo: this is temp
                    labelFilters: _options.GraphSample.LabelFilters,
                    nodeSamplingCountAtRoot: _options.GraphSample.ForestFireNodeSamplingCountAtRoot,
                    maxHops: _options.GraphSample.ForestFireMaxHops,
                    queryLimit: _options.GraphSample.ForestFireQueryLimit,
                    nodeCountReductionFactorByHop: _options.GraphSample.ForestFireNodeCountReductionFactorByHop);
                var perBatchLabelsFilename = Path.Join(_options.WorkingDir, "Labels.tsv");

                if (graph.NodeCount < _options.GraphSample.MinNodeCount - (_options.GraphSample.MinNodeCount * 0.0) ||
                    graph.EdgeCount < _options.GraphSample.MinEdgeCount - (_options.GraphSample.MinEdgeCount * 0.0))
                {
                    _logger.LogError(
                        "The sampled graph with {a} nodes and {b} edges does not match required charactersitics: " +
                        "MinNodeCount: {c}, MaxNodeCount: {d}, MinEdgeCount: {e}, MaxEdgeCount: {f}",
                        graph.NodeCount,
                        graph.EdgeCount,
                        _options.GraphSample.MinNodeCount,
                        _options.GraphSample.MaxNodeCount,
                        _options.GraphSample.MinEdgeCount,
                        _options.GraphSample.MaxEdgeCount);

                    _logger.LogError(
                        "Failed sampling neighbors of the root node {r}.",
                        ((ScriptNode)rootNode).Address);

                    return;
                }
                else
                {
                    graph.Serialize(
                        Path.Join(_options.WorkingDir, graph.Id),
                        perBatchLabelsFilename,
                        serializeFeatureVectors: _options.GraphSample.SerializeFeatureVectors,
                        serializeEdges: _options.GraphSample.SerializeEdges);

                    _logger.LogInformation("Serialized the graph.");

                    sampledGraphsCounter++;
                }
            }
        }
    }

    private List<Model.INode> ProcessSamplingResult(List<IRecord> samplingResult, int hop, string rootScriptAddress, HashSet<string> allNodesAddedToGraph, HashSet<string> allEdgesAddedToGraph, string rootNodeId, int nodeSamplingCountAtRoot, double nodeCountReductionFactorByHop, BitcoinGraph g)
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

        // TODO: this iteration needs to be improved, maybe I have a list like this because of the query?!
        foreach (var r in samplingResult)
        {
            if (rootScriptAddress == BitcoinAgent.Coinbase)
            {
                // ********
                //root = new CoinbaseNode(r.Values["root"].As<List<Neo4j.Driver.INode>>()[0]);
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
                // ********
                //var rootB = r.Values["root"].As<List<Neo4j.Driver.INode>>()[0];
                var rootList = r["root"].As<List<object>>();
                (Neo4j.Driver.INode rootB, double inDegree, double outDegree, double outHopsFromRoot) = UnpackDict(rootList[0].As<IDictionary<string, object>>(), hop);

                if (rootB is null)
                    continue;

                root = new ScriptNode(rootB, originalIndegree: inDegree, originalOutdegree: outDegree, outHopsFromRoot: outHopsFromRoot);

                if (!allNodesAddedToGraph.Contains(rootB.ElementId))
                {
                    g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, root);
                    allNodesAddedToGraph.Add(rootB.ElementId);
                }

                rootNodeId = root.Id;
            }

            // ********
            /*
            foreach (var node in r.Values["nodes"].As<List<Neo4j.Driver.INode>>())
                if (!allNodesAddedToGraph.Contains(node.ElementId))
                    nodes.TryAdd(node.ElementId, node);*/

            foreach (var nodeObject in r["nodes"].As<List<object>>())
            {
                (Neo4j.Driver.INode node, double inDegree, double outDegree, double hopsFromRoot) = UnpackDict(nodeObject.As<IDictionary<string, object>>(), hop);
                //g.GetOrAddNode(node, originalIndegree: inDegree, originalOutdegree: outDegree);

                if (!allNodesAddedToGraph.Contains(node.ElementId))
                    nodes.TryAdd(node.ElementId, (node, inDegree, outDegree, hopsFromRoot));
            }

            foreach (var edge in r.Values["relationships"].As<List<IRelationship>>())
                if (!allEdgesAddedToGraph.Contains(edge.ElementId))
                    edges.TryAdd(edge.ElementId, edge);
        }

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
                addedNodes.Add(g.GetOrAddNode(ccNode, originalIndegree: indegree, originalOutdegree: outdegree, outHopsFromRoot: outHopsFromRoot));
                allNodesAddedToGraph.Add(targetNodeId);

                g.GetOrAddEdge(edge.Value);
                allEdgesAddedToGraph.Add(edge.Value.ElementId);
            }
        }

        return addedNodes;
    }


    private async Task ProcessHops(string rootNodeLabel, string propKey, string propValue, int maxHops, int hop, string rootScriptAddress, int queryLimit, string labelFilters, HashSet<string> allNodesAddedToGraph, HashSet<string> allEdgesAddedToGraph, string rootNodeId, int nodeSamplingCountAtRoot, double nodeCountReductionFactorByHop, BitcoinGraph g)
    {
        var samplingResult = await _graphDb.GetNeighbors(rootNodeLabel, propKey, propValue, queryLimit, labelFilters, 1, SamplingAlgorithm.BFS);

        var selectedNodes = ProcessSamplingResult(
            samplingResult, hop,
            rootScriptAddress: rootScriptAddress,
            allNodesAddedToGraph: allNodesAddedToGraph,
            allEdgesAddedToGraph: allEdgesAddedToGraph,
            rootNodeId: rootNodeId,
            nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
            nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
            g: g);

        if (hop < maxHops)
        {
            foreach (var node in selectedNodes)
                if (node.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode) // TODO: this is currently a limitation since we currently do not support root nodes of other types.
                    await ProcessHops(rootNodeLabel: ScriptNodeStrategy.Labels, propKey: "Address", propValue: ((ScriptNode)node).Address, hop: hop + 1, maxHops: maxHops, rootScriptAddress: rootScriptAddress, queryLimit: queryLimit, labelFilters: labelFilters, allNodesAddedToGraph: allNodesAddedToGraph, allEdgesAddedToGraph: allEdgesAddedToGraph, rootNodeId: rootNodeId, nodeSamplingCountAtRoot: nodeSamplingCountAtRoot, nodeCountReductionFactorByHop: nodeCountReductionFactorByHop, g: g);
        }
    }

    private async Task<GraphBase> GetNeighbors(
        string rootScriptAddress,
        int nodeSamplingCountAtRoot,
        int maxHops,
        int queryLimit,
        double nodeCountReductionFactorByHop,
        string labelFilters)
    {
        var g = new BitcoinGraph();
        var rootNodeId = "";
        var allNodesAddedToGraph = new HashSet<string>();
        var allEdgesAddedToGraph = new HashSet<string>();

        // TODO: the whole method of using 'Coinbase' to alter the functionality seems hacky
        // need to find a better solution.

        _logger.LogInformation("Getting neighbors of random node {node}, at {hop} hop distance.", rootScriptAddress, maxHops);

        await ProcessHops(
            rootNodeLabel: rootScriptAddress == BitcoinAgent.Coinbase ? BitcoinAgent.Coinbase : ScriptNodeStrategy.Labels,
            propKey: "Address",
            propValue: rootScriptAddress,
            maxHops: maxHops,
            hop: 0,
            rootScriptAddress: rootScriptAddress,
            queryLimit: queryLimit,
            labelFilters: labelFilters,
            allNodesAddedToGraph: allNodesAddedToGraph,
            allEdgesAddedToGraph: allEdgesAddedToGraph,
            rootNodeId: rootNodeId,
            nodeSamplingCountAtRoot: nodeSamplingCountAtRoot,
            nodeCountReductionFactorByHop: nodeCountReductionFactorByHop,
            g: g);

        _logger.LogInformation("Retrieved neighbors.");
        _logger.LogInformation("Building a graph from the neighbors.");

        _logger.LogInformation("Build graph from the neighbors; {nodeCount} nodes and {edgeCount} edges.", g.NodeCount, g.EdgeCount);

        g.AddLabel("ConnectedGraph_or_Forest", "1");
        g.AddLabel("RootNodeId", rootNodeId);

        return g;
    }
}