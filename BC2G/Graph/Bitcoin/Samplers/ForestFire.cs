using BC2G.Graph.Db.Neo4jDb.Bitcoin.Strategies;
using BC2G.Utilities;

namespace BC2G.Graph.Bitcoin.Samplers;

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

                if (await TrySampleNeighborsAsync(driver, rootNode, baseOutputDir))
                {
                    sampledGraphsCounter++;
                    _logger.LogInformation("Finished writting sampled graph features.");
                }
                else
                {
                    _logger.LogError("Failed sampling neighbors of the root node {r}.", rootNode.Address);
                }
            }
        }
    }

    // TODO: most likely this method can be simplified and merged with the above method.

    private async Task<bool> TrySampleNeighborsAsync(
        IDriver driver, ScriptNode rootNode, string workingDir)
    {
        var graph = await GetNeighborsAsync(driver, rootNode.Address, Options.GraphSample);
        var perBatchLabelsFilename = Path.Join(workingDir, "Labels.tsv");

        if (!CanUseGraph(
            graph, tolerance: 0,
            minNodeCount: Options.GraphSample.MinNodeCount,
            maxNodeCount: Options.GraphSample.MaxNodeCount,
            minEdgeCount: Options.GraphSample.MinEdgeCount,
            maxEdgeCount: Options.GraphSample.MaxEdgeCount))
        {
            Logger.LogError(
                "The sampled graph with {a} nodes and {b} edges does not match required charactersitics: " +
                "MinNodeCount: {c}, MaxNodeCount: {d}, MinEdgeCount: {e}, MaxEdgeCount: {f}",
                graph.NodeCount,
                graph.EdgeCount,
                Options.GraphSample.MinNodeCount,
                Options.GraphSample.MaxNodeCount,
                Options.GraphSample.MinEdgeCount,
                Options.GraphSample.MaxEdgeCount);
            return false;
        }

        graph.Serialize(
            Path.Join(workingDir, graph.Id),
            perBatchLabelsFilename,
            serializeFeatureVectors: Options.GraphSample.SerializeFeatureVectors,
            serializeEdges: Options.GraphSample.SerializeEdges);

        Logger.LogInformation("Serialized the graph.");

        return true;
    }

    private async Task<GraphBase> GetNeighborsUsingForestFireSamplingAlgorithmAsync(
        IDriver driver,
        string rootScriptAddress,
        int nodeSamplingCountAtRoot,
        int maxHops,
        int queryLimit,
        double nodeCountReductionFactorByHop,
        string labelFilters)
    {
        // TODO: this method is experimental, need a thorough re-write.

        static (Neo4j.Driver.INode, double, double, double) UnpackDict(IDictionary<string, object> dict, double hop)
        {
            var node = dict["node"].As<Neo4j.Driver.INode>();
            var inDegree = Convert.ToDouble(dict["inDegree"]);
            var outDegree = Convert.ToDouble(dict["outDegree"]);
            return (node, inDegree, outDegree, hop);
        }

        var rnd = new Random(31);
        var g = new BitcoinGraph();
        var rootNodeId = "";
        var allNodesAddedToGraph = new HashSet<string>();
        var allEdgesAddedToGraph = new HashSet<string>();
        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        string GetNeighborsQuery(string rootNode)
        {
            var qBuilder = new StringBuilder();
            qBuilder.Append(rootNode);

            qBuilder.Append($"CALL apoc.path.spanningTree(root, {{");
            qBuilder.Append($"maxLevel: 1, ");
            qBuilder.Append($"limit: {queryLimit}, ");
            qBuilder.Append($"bfs: true, ");
            qBuilder.Append($"labelFilter: '{labelFilters}'");
            //$"    relationshipFilter: \">{EdgeType.Transfers}\"" +
            qBuilder.Append($"}}) ");
            qBuilder.Append($"YIELD path ");
            qBuilder.Append($"WITH root, ");
            qBuilder.Append($"nodes(path) AS pathNodes, ");
            qBuilder.Append($"relationships(path) AS pathRels ");
            qBuilder.Append($"LIMIT {queryLimit} ");
            //qBuilder.Append($"RETURN [root] AS root, [n IN pathNodes WHERE n <> root] AS nodes, pathRels AS relationships");
            // ******** 
            qBuilder.Append($"RETURN ");
            qBuilder.Append($"[ {{");
            qBuilder.Append($"node: root, ");
            qBuilder.Append($"inDegree: COUNT {{ (root)<--() }}, ");
            qBuilder.Append($"outDegree: COUNT {{ (root)-->() }} ");
            qBuilder.Append($"}}] AS root, ");
            qBuilder.Append($"[ ");
            qBuilder.Append($"n IN pathNodes WHERE n <> root ");
            qBuilder.Append($"| ");
            qBuilder.Append($"{{ ");
            qBuilder.Append($"node: n, ");
            qBuilder.Append($"inDegree: COUNT {{ (n)<--() }}, ");
            qBuilder.Append($"outDegree: COUNT {{ (n)-->() }} ");
            qBuilder.Append($"}} ");
            qBuilder.Append($"] AS nodes, ");
            qBuilder.Append($"pathRels AS relationships");

            return qBuilder.ToString();
        }

        List<Model.INode> ProcessSamplingResult(List<IRecord> samplingResult, int hop)
        {
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

        async Task ProcessHops(List<string> getNeighborsQueries, int hop = 0)
        {
            foreach (var q in getNeighborsQueries)
            {
                var samplingResult = await session.ExecuteReadAsync(async x =>
                {
                    var result = await x.RunAsync(q);
                    return await result.ToListAsync();
                });

                var selectedNodes = ProcessSamplingResult(samplingResult, hop);

                if (hop < maxHops)
                {
                    var queries = new List<string>();
                    foreach (var node in selectedNodes)
                        if (node.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode) // TODO: this is currently a limitation since we currently do not support root nodes of other types.
                            queries.Add(GetNeighborsQuery($"MATCH (root:{ScriptNodeStrategy.Labels} {{ Address: \"{((ScriptNode)node).Address}\" }}) "));

                    await ProcessHops(queries, hop + 1);
                }
            }
        }

        // TODO: the whole method of using 'Coinbase' to alter the functionality seems hacky
        // need to find a better solution.

        Logger.LogInformation("Getting neighbors of random node {node}, at {hop} hop distance.", rootScriptAddress, maxHops);


        var getRootNodeNeighborsQuery = new List<string>()
        {
            GetNeighborsQuery(
                rootScriptAddress == BitcoinAgent.Coinbase ?
                $"MATCH (root:{BitcoinAgent.Coinbase}) " :
                $"MATCH (root:{ScriptNodeStrategy.Labels} {{ Address: \"{rootScriptAddress}\" }}) ")
        };

        await ProcessHops(getRootNodeNeighborsQuery);

        Logger.LogInformation("Retrieved neighbors.");
        Logger.LogInformation("Building a graph from the neighbors.");

        Logger.LogInformation("Build graph from the neighbors; {nodeCount} nodes and {edgeCount} edges.", g.NodeCount, g.EdgeCount);

        g.AddLabel("ConnectedGraph_or_Forest", "1");
        g.AddLabel("RootNodeId", rootNodeId);

        return g;
    }
}
