using EBA.Graph.Bitcoin;
using EBA.Graph.Db.Neo4jDb;
using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;
using EBA.Utilities;
using EBA.Utilities;
using Microsoft.Extensions.Primitives;
using System.Reflection.Emit;


namespace EBA.Graph.Db.Neo4jDb.Bitcoin;

public class BitcoinNeo4jDbLegacy : Neo4jDbLegacy<BitcoinGraph>
{
    public BitcoinNeo4jDbLegacy(Options options, ILogger<BitcoinNeo4jDbLegacy> logger) :
        base(options, logger, new BitcoinStrategyFactory(options))
    { }

    public override async Task<IDriver> GetDriver(Neo4jOptions options)
    {
        var driver = await base.GetDriver(options);
        await EnsureCoinbaseNodeAsync(driver);
        await CreateIndexesAndConstraintsAsync(driver);

        return driver;
    }

    public override async Task SerializeAsync(BitcoinGraph g, CancellationToken ct)
    {
        var nodes = g.GetNodes();
        var edges = g.GetEdges();
        var graphType = BitcoinGraph.ComponentType;
        var batchInfo = await GetBatchAsync(
            nodes.Keys.Concat(edges.Keys).Append(graphType).ToList());

        var tasks = new List<Task>();

        batchInfo.AddOrUpdate(graphType, 1);
        var graphStrategy = StrategyFactory.GetStrategy(graphType);
        tasks.Add(graphStrategy.ToCsvAsync(g, batchInfo.GetFilename(graphType)));

        foreach (var type in nodes)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count(x => x.Id != NodeLabels.Coinbase.ToString()));
            var _strategy = StrategyFactory.GetStrategy(type.Key);
            tasks.Add(
                _strategy.ToCsvAsync(
                    type.Value.Where(x => x.Id != NodeLabels.Coinbase.ToString()),
                    batchInfo.GetFilename(type.Key)));
        }

        foreach (var type in edges)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count);
            var _strategy = StrategyFactory.GetStrategy(type.Key);
            tasks.Add(
                _strategy.ToCsvAsync(
                    type.Value,
                    batchInfo.GetFilename(type.Key)));
        }

        await Task.WhenAll(tasks);
    }

    public override Task ImportAsync(CancellationToken ct, string batchName = "", List<GraphComponentType>? importOrder = null)
    {
        importOrder ??= new List<GraphComponentType>()
        {
            GraphComponentType.BitcoinGraph,
            GraphComponentType.BitcoinScriptNode,
            GraphComponentType.BitcoinTxNode,
            GraphComponentType.BitcoinC2S,
            GraphComponentType.BitcoinC2T,
            GraphComponentType.BitcoinS2S,
            GraphComponentType.BitcoinT2T
        };
        return base.ImportAsync(ct, batchName, importOrder);
    }

    public override async Task SampleAsync(CancellationToken ct)
    {
        var driver = await GetDriver(Options.Neo4j);

        var sampledGraphsCounter = 0;
        var attempts = 0;
        var baseOutputDir = Path.Join(Options.WorkingDir, $"sampled_graphs_{Helpers.GetUnixTimeSeconds()}");

        // creating a script node like the following just to ask for coinbase node is not ideal
        // TODO: find a better solution.
        // TODO: this is a bitcoin-specific logic and should not be here.
        if (Options.Bitcoin.GraphSample.CoinbaseMode != CoinbaseSelectionMode.ExcludeCoinbase)
        {
            Logger.LogInformation("Sampling neighbors of the coinbase node.");
            var tmpSolutionCoinbase = new ScriptNode(NodeLabels.Coinbase.ToString(), ScriptType.Coinbase);
            if (await TrySampleNeighborsAsync(driver, tmpSolutionCoinbase, baseOutputDir))
            {
                sampledGraphsCounter++;
                Logger.LogInformation("Finished writting sampled graph of coinbase neighbors.");
            }
            else
            {
                Logger.LogError("Failed sampling neighbors of the coinbase node.");
            }
        }

        if (Options.Bitcoin.GraphSample.CoinbaseMode != CoinbaseSelectionMode.CoinbaseOnly)
        {
            Logger.LogInformation("Sampling {n} graphs.", Options.Bitcoin.GraphSample.Count - sampledGraphsCounter);

            while (
                sampledGraphsCounter < Options.Bitcoin.GraphSample.Count &&
                ++attempts <= Options.Bitcoin.GraphSample.MaxAttempts)
            {
                Logger.LogInformation(
                    "Getting {n} random root nodes; attempt {a}/{m}.",
                    Options.Bitcoin.GraphSample.Count - sampledGraphsCounter,
                    attempts, Options.Bitcoin.GraphSample.MaxAttempts);

                var rndRootNodes = await GetRandomNodes(
                    driver,
                    Options.Bitcoin.GraphSample.Count - sampledGraphsCounter,
                    Options.Bitcoin.GraphSample.RootNodeSelectProb);

                Logger.LogInformation("Selected {n} random root nodes.", rndRootNodes.Count);

                int counter = 0;
                foreach (var rootNode in rndRootNodes)
                {
                    Logger.LogInformation("Sampling neighbors of the random root node {n}/{t}.", ++counter, rndRootNodes.Count);

                    if (await TrySampleNeighborsAsync(driver, rootNode, baseOutputDir))
                    {
                        sampledGraphsCounter++;
                        Logger.LogInformation("Finished writting sampled graph features.");
                    }
                    else
                    {
                        Logger.LogError("Failed sampling neighbors of the root node {r}.", rootNode.Address);
                    }
                }
            }

            if (attempts > Options.Bitcoin.GraphSample.MaxAttempts)
            {
                Logger.LogError(
                    "Failed creating {g} {g_msg} after {a} {a_msg}; created {c} {c_msg}. " +
                    "You may retry, and if the error persists, try adjusting the values of " +
                    "{minN}={minNV}, {maxN}={maxNV}, {minE}={minEV}, and {maxE}={maxEV}.",
                    Options.Bitcoin.GraphSample.Count,
                    Options.Bitcoin.GraphSample.Count > 1 ? "graphs" : "graph",
                    attempts - 1,
                    attempts > 1 ? "attempts" : "attempt",
                    sampledGraphsCounter,
                    sampledGraphsCounter > 1 ? "graphs" : "graph",
                    nameof(Options.Bitcoin.GraphSample.MinNodeCount), Options.Bitcoin.GraphSample.MinNodeCount,
                    nameof(Options.Bitcoin.GraphSample.MaxNodeCount), Options.Bitcoin.GraphSample.MaxNodeCount,
                    nameof(Options.Bitcoin.GraphSample.MinEdgeCount), Options.Bitcoin.GraphSample.MinEdgeCount,
                    nameof(Options.Bitcoin.GraphSample.MaxEdgeCount), Options.Bitcoin.GraphSample.MaxEdgeCount);
                return;
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }
    }

    public override void ReportQueries()
    {
        var supportedComponentTypes = new GraphComponentType[]
        {
            GraphComponentType.BitcoinGraph,
            GraphComponentType.BitcoinScriptNode,
            GraphComponentType.BitcoinTxNode,
            GraphComponentType.BitcoinC2S,
            GraphComponentType.BitcoinC2T,
            GraphComponentType.BitcoinS2S,
            GraphComponentType.BitcoinT2T
        };

        foreach (GraphComponentType gcType in supportedComponentTypes)
        {
            var strategy = StrategyFactory.GetStrategy(gcType);
            var filename = Path.Join(Options.WorkingDir, $"cypher_query_{gcType}.cypher");
            using var writer = new StreamWriter(filename);
            writer.WriteLine(strategy.GetQuery("file:///filename_under_dbms_directories_import"));

            Logger.LogInformation("Serialized cypher query for {gcType} to {filename}.", gcType.ToString(), filename);
        }

        Logger.LogInformation("Finished serializing all cypher queries for Bitcoin graph.");
    }

    public override async Task<List<ScriptNode>> GetRandomNodes(
        IDriver driver, int nodesCount, double rootNodesSelectProb = 0.1)
    {
        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var rndNodeVar = "rndScript";
        var rndRecords = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH ({rndNodeVar}:{ScriptNodeStrategy.Label}) " +
                $"WHERE rand() < {rootNodesSelectProb} " +
                $"WITH {rndNodeVar} " +
                $"ORDER BY rand() " +
                $"LIMIT {nodesCount} " +
                $"RETURN {rndNodeVar}");

            return await result.ToListAsync();
        });

        var rndNodes = new List<ScriptNode>();
        foreach (var n in rndRecords)
            rndNodes.Add(ScriptNodeStrategy.GetNodeFromProps(n.Values[rndNodeVar].As<Neo4j.Driver.INode>(), null, null, null));

        return rndNodes;
    }

    public override async Task<bool> TrySampleNeighborsAsync(
        IDriver driver, ScriptNode rootNode, string workingDir)
    {
        var graph = await GetNeighborsAsync(driver, rootNode.Address, Options.Bitcoin.GraphSample);
        var perBatchLabelsFilename = Path.Join(workingDir, "metadata.tsv");

        if (!CanUseGraph(
            graph, tolerance: 0,
            minNodeCount: Options.Bitcoin.GraphSample.MinNodeCount,
            maxNodeCount: Options.Bitcoin.GraphSample.MaxNodeCount,
            minEdgeCount: Options.Bitcoin.GraphSample.MinEdgeCount,
            maxEdgeCount: Options.Bitcoin.GraphSample.MaxEdgeCount))
        {
            Logger.LogError(
                "The sampled graph with {a} nodes and {b} edges does not match required charactersitics: " +
                "MinNodeCount: {c}, MaxNodeCount: {d}, MinEdgeCount: {e}, MaxEdgeCount: {f}",
                graph.NodeCount,
                graph.EdgeCount,
                Options.Bitcoin.GraphSample.MinNodeCount,
                Options.Bitcoin.GraphSample.MaxNodeCount,
                Options.Bitcoin.GraphSample.MinEdgeCount,
                Options.Bitcoin.GraphSample.MaxEdgeCount);
            return false;
        }

        if (Options.Bitcoin.GraphSample.Mode == GraphSampleMode.ConnectedGraphAndForest)
        {
            //var disjointGraphs = await GetDisjointGraphsRandomEdges(driver, graph.EdgeCount);
            var disjointGraphs = await GetDisjointGraphsRandomNeighborhoods(driver, graph.EdgeCount, Options.Bitcoin.GraphSample);

            if (!CanUseGraph(
                disjointGraphs,
                minNodeCount: Options.Bitcoin.GraphSample.MinNodeCount,
                maxNodeCount: graph.GetNodeCount(GraphComponentType.BitcoinScriptNode),
                minEdgeCount: Options.Bitcoin.GraphSample.MinEdgeCount,
                maxEdgeCount: graph.EdgeCount))
            {
                Logger.LogError(
                    "The sampled disjoint graph with {a} nodes and {b} edges does not match required charactersitics: " +
                    "MinNodeCount: {c}, MaxNodeCount: {d}, MinEdgeCount: {e}, MaxEdgeCount: {f}",
                    graph.NodeCount,
                    graph.EdgeCount,
                    Options.Bitcoin.GraphSample.MinNodeCount,
                    Options.Bitcoin.GraphSample.MaxNodeCount,
                    Options.Bitcoin.GraphSample.MinEdgeCount,
                    Options.Bitcoin.GraphSample.MaxEdgeCount);

                return false;
            }

            disjointGraphs.Serialize(
                Path.Join(workingDir, disjointGraphs.Id),
                perBatchLabelsFilename,
                serializeFeatureVectors: Options.Bitcoin.GraphSample.SerializeFeatureVectors,
                serializeEdges: Options.Bitcoin.GraphSample.SerializeEdges);
        }

        graph.Serialize(
            Path.Join(workingDir, graph.Id),
            perBatchLabelsFilename,
            serializeFeatureVectors: Options.Bitcoin.GraphSample.SerializeFeatureVectors,
            serializeEdges: Options.Bitcoin.GraphSample.SerializeEdges);

        Logger.LogInformation("Serialized the graph.");

        return true;
    }

    public override async Task<GraphBase> GetNeighborsAsync(
        IDriver driver, string rootScriptAddress, BitcoinGraphSampleOptions options)
    {
        // TODO: both of the following methods need a rewrite, they could be merged with simpler interface.

        /*if (options.TraversalAlgorithm == GraphTraversal.BFS || options.TraversalAlgorithm == GraphTraversal.DFS)
            return await GetNeighborsUsingGraphTraversalAlgorithmAsync(driver, rootScriptAddress, options);
        */
        return
            await GetNeighborsUsingForestFireSamplingAlgorithmAsync(
            driver: driver,
            rootScriptAddress: rootScriptAddress,
            nodeSamplingCountAtRoot: options.ForestFireOptions.NodeSamplingCountAtRoot,
            maxHops: options.ForestFireOptions.MaxHops,
            queryLimit: options.ForestFireOptions.QueryLimit,
            nodeCountReductionFactorByHop: options.ForestFireOptions.NodeCountReductionFactorByHop);
    }

    private async Task<GraphBase> GetNeighborsUsingGraphTraversalAlgorithmAsync(IDriver driver, string rootScriptAddress, BitcoinGraphSampleOptions options)
    {
        // TODO: the whole method of using 'Coinbase' to alter the functionality seems hacky
        // need to find a better solution.

        Logger.LogInformation("Getting neighbors of random node {node}, at {hop} hop distance.", rootScriptAddress, options.Hops);

        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var qBuilder = new StringBuilder();
        if (rootScriptAddress == NodeLabels.Coinbase.ToString())
            qBuilder.Append($"MATCH (root:{NodeLabels.Coinbase.ToString()}) ");
        else
            qBuilder.Append($"MATCH (root:{ScriptNodeStrategy.Label} {{ Address: \"{rootScriptAddress}\" }}) ");

        qBuilder.Append($"CALL apoc.path.spanningTree(root, {{");
        qBuilder.Append($"maxLevel: {options.Hops}, ");
        qBuilder.Append($"limit: {Options.Bitcoin.GraphSample.MaxEdgesFetchFromNeighbor}, ");

        //if (Options.Bitcoin.GraphSample.TraversalAlgorithm == GraphTraversal.BFS)
            qBuilder.Append($"bfs: true, ");
        /*else
            qBuilder.Append($"bfs: false, ");*/

        //qBuilder.Append($"labelFilter: '{options.LabelFilters}'");
        //$"    relationshipFilter: \">{EdgeType.Transfers}\"" +
        qBuilder.Append($"}}) ");
        qBuilder.Append($"YIELD path ");
        qBuilder.Append($"WITH root, ");
        qBuilder.Append($"nodes(path) AS pathNodes, ");
        qBuilder.Append($"relationships(path) AS pathRels ");
        //qBuilder.Append($"WHERE size(pathNodes) <= {options.MaxNodeCount} AND size(pathRels) <= {options.MaxEdgeCount} ");
        qBuilder.Append($"LIMIT {Options.Bitcoin.GraphSample.MaxNodeFetchFromNeighbor} ");

        // ******** 
        //qBuilder.Append($"RETURN [root] AS root, [n IN pathNodes WHERE n <> root] AS nodes, pathRels AS relationships");

        // The following part is to get the inDegree and outDegree of each node in the original graph, NOT the sampled graph.
        // It basically iterates over the nodes and fetches their in and out degree from the graph. 
        // This is an expensive operation, and if these degrees are not needed, replace all the following with the
        // above line marked with ******** and make sure the marked lines in the following are also updated.
        //
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


        var q = qBuilder.ToString();

        var samplingResult = await session.ExecuteReadAsync(async x =>
        {
            //var result = await x.RunAsync(
            //    $"MATCH path = (p: {ScriptNodeStrategy.Labels} {{ Address: \"{rootScriptAddress}\"}}) -[* 1..{maxHops}]->(p2) " +
            //    "WITH p, [n in nodes(path) where n <> p | n] as nodes, relationships(path) as relationships " +
            //    "WITH collect(distinct p) as root, size(nodes) as cnt, collect(nodes[-1]) as nodes, collect(distinct relationships[-1]) as relationships " +
            //    "RETURN root, nodes, relationships");

            var result = await x.RunAsync(q);

            // Note:
            // Neo4j has apoc.neighbors.byhop method that returns
            // neighbors at n-hop distance. However, this method
            // does not return relationships, therefore, the above
            // cypher query is used instead.
            //
            // TODO:
            // Modify the above cypher query to return only one root,
            // it currently returns one root per hop where root nodes
            // of all the hops are equal.

            return await result.ToListAsync();
        });

        Logger.LogInformation("Retrieved neighbors.");
        Logger.LogInformation("Building a graph from the neighbors.");

        static (Neo4j.Driver.INode, double, double) UnpackDict(IDictionary<string, object> dict)
        {
            var node = dict["node"].As<Neo4j.Driver.INode>();
            var inDegree = Convert.ToDouble(dict["inDegree"]);
            var outDegree = Convert.ToDouble(dict["outDegree"]);
            return (node, inDegree, outDegree);
        }

        var g = new BitcoinGraph();
        var rootNodeId = "";

        foreach (var hop in samplingResult)
        {
            Node root;
            if (rootScriptAddress == NodeLabels.Coinbase.ToString())
            {
                // ********
                //root = new CoinbaseNode(hop.Values["root"].As<List<Neo4j.Driver.INode>>()[0]);
                var rootList = hop["root"].As<List<object>>();
                (Neo4j.Driver.INode rootNode, double inDegree, double outDegree) = UnpackDict(rootList[0].As<IDictionary<string, object>>());
                root = new CoinbaseNode(rootNode, originalOutdegree: outDegree);
                if (root is null)
                    continue;

                g.GetOrAddNode(GraphComponentType.BitcoinCoinbaseNode, root);
                rootNodeId = root.Id;
            }
            else
            {
                // ********
                //root = new ScriptNode(hop.Values["root"].As<List<Neo4j.Driver.INode>>()[0]);
                var rootList = hop["root"].As<List<object>>();
                (Neo4j.Driver.INode rootNode, double inDegree, double outDegree) = UnpackDict(rootList[0].As<IDictionary<string, object>>());
                root = ScriptNodeStrategy.GetNodeFromProps(rootNode, originalIndegree: inDegree, originalOutdegree: outDegree, hopsFromRoot: null);
                if (root is null)
                    continue;

                g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, root);
                rootNodeId = root.Id;
            }

            // It is better to add nodes like this, and not just as part of 
            // adding edge, because `nodes` has all the node properties for each 
            // node, but `relationships` only contain their IDs.
            // ********
            //foreach (var node in hop.Values["nodes"].As<List<Neo4j.Driver.INode>>())
            //    g.GetOrAddNode(node);
            foreach (var nodeObject in hop["nodes"].As<List<object>>())
            {
                (Neo4j.Driver.INode node, double inDegree, double outDegree) = UnpackDict(nodeObject.As<IDictionary<string, object>>());
                NodeFactory.TryCreateNode(node, originalIndegree: inDegree, originalOutdegree: outDegree, outHopsFromRoot: 0, out var v);
                g.GetOrAddNode(v);
            }

            foreach (var relationship in hop.Values["relationships"].As<List<IRelationship>>())
                g.GetOrAddEdge(relationship);
        }

        Logger.LogInformation("Build graph from the neighbors; {nodeCount} nodes and {edgeCount} edges.", g.NodeCount, g.EdgeCount);

        g.AddLabel("ConnectedGraph_or_Forest", "1");
        g.AddLabel("RootNodeId", rootNodeId);

        return g;
    }

    private async Task<GraphBase> GetNeighborsUsingForestFireSamplingAlgorithmAsync(
        IDriver driver,
        string rootScriptAddress,
        int nodeSamplingCountAtRoot,
        int maxHops,
        int queryLimit,
        double nodeCountReductionFactorByHop)
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
            //qBuilder.Append($"labelFilter: '{labelFilters}'");
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
                if (rootScriptAddress == NodeLabels.Coinbase.ToString())
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

                    root = ScriptNodeStrategy.GetNodeFromProps(rootB, originalIndegree: inDegree, originalOutdegree: outDegree, hopsFromRoot: outHopsFromRoot);

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
                    NodeFactory.TryCreateNode(ccNode, originalIndegree: indegree, originalOutdegree: outdegree, outHopsFromRoot: outHopsFromRoot, out var v);
                    addedNodes.Add(v);
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
                            queries.Add(GetNeighborsQuery($"MATCH (root:{ScriptNodeStrategy.Label} {{ Address: \"{((ScriptNode)node).Address}\" }}) "));

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
                rootScriptAddress == NodeLabels.Coinbase.ToString() ?
                $"MATCH (root:{NodeLabels.Coinbase.ToString()}) " :
                $"MATCH (root:{ScriptNodeStrategy.Label} {{ Address: \"{rootScriptAddress}\" }}) ")
        };

        await ProcessHops(getRootNodeNeighborsQuery);

        Logger.LogInformation("Retrieved neighbors.");
        Logger.LogInformation("Building a graph from the neighbors.");

        Logger.LogInformation("Build graph from the neighbors; {nodeCount} nodes and {edgeCount} edges.", g.NodeCount, g.EdgeCount);

        g.AddLabel("ConnectedGraph_or_Forest", "1");
        g.AddLabel("RootNodeId", rootNodeId);

        return g;
    }

    public override async Task<GraphBase> GetDisjointGraphsRandomEdges(
        IDriver driver, int edgeCount, double edgeSelectProb = 0.2)
    {
        using var session = driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Read));

        var randomNodes = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"Match (source:{ScriptNodeStrategy.Label})-[edge:{EdgeType.Transfers}]->(target:{ScriptNodeStrategy.Label}) " +
                $"where rand() < {edgeSelectProb} " +
                $"return source, edge, target limit {edgeCount}");

            return await result.ToListAsync();
        });

        var g = new BitcoinGraph();

        foreach (var n in randomNodes)
        {
            g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, ScriptNodeStrategy.GetNodeFromProps(n.Values["source"].As<Neo4j.Driver.INode>(), null, null, null));
            g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, ScriptNodeStrategy.GetNodeFromProps(n.Values["target"].As<Neo4j.Driver.INode>(), null, null, null));
            g.GetOrAddEdge(n.Values["edge"].As<IRelationship>());
        }

        g.AddLabel("ConnectedGraph_or_Forest", "0");

        return g;
    }

    public override async Task<GraphBase> GetDisjointGraphsRandomNeighborhoods(
        IDriver driver, int edgeCount, BitcoinGraphSampleOptions options)
    {
        var g = new BitcoinGraph();

        Logger.LogInformation("Getting random root nodes.");
        var rndRootNodes = await GetRandomNodes(driver, 100);
        Logger.LogInformation("Got {count} random root nodes.", rndRootNodes.Count);

        foreach (var rndRootNode in rndRootNodes)
        {
            // other formats are not supported yet, do not change this label filter options.LabelFilters);
            var gB = await GetNeighborsUsingForestFireSamplingAlgorithmAsync(
                driver: driver,
                rootScriptAddress: rndRootNode.Address,
                nodeSamplingCountAtRoot: options.DisjointGraph_ForestFireNodeSamplingCountAtRoot,
                maxHops: options.DisjointGraph_ForestFireMaxHops,
                queryLimit: options.DisjointGraph_ForestFireQueryLimit,
                nodeCountReductionFactorByHop: options.DisjointGraph_ForestFireNodeCountReductionFactorByHop);

            foreach (var node in gB.GetNodes())
                foreach (var n in node.Value)
                    g.GetOrAddNode(node.Key, n);

            foreach (var edge in gB.GetEdges())
                foreach (var e in edge.Value)
                    g.TryGetOrAddEdge(edge.Key, e, out _);

            if (g.EdgeCount + g.EdgeCount * 0.2 >= options.MaxEdgeCount || g.NodeCount + g.NodeCount * 0.2 >= options.MaxNodeCount)
                break;
        }

        g.AddLabel("ConnectedGraph_or_Forest", "0");

        return g;
    }

    private static async Task EnsureCoinbaseNodeAsync(IDriver driver)
    {
        int count = 0;
        using (var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read)))
        {
            count = await session.ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync($"MATCH (n:{Blockchains.Bitcoin.BitcoinChainAgent.Coinbase}) RETURN COUNT(n)");
                return result.SingleAsync().Result[0].As<int>();
            });
        }

        switch (count)
        {
            case 1: return;
            case 0:
                using (var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write)))
                {
                    await session.ExecuteWriteAsync(async tx =>
                    {
                        var x = PropertyMappingFactory.Address<ScriptNode>(n => n.Address).Property.Name;
                        await tx.RunAsync(
                            $"CREATE (:{Blockchains.Bitcoin.BitcoinChainAgent.Coinbase} {{" +
                            $"{x}: " +
                            $"\"{Blockchains.Bitcoin.BitcoinChainAgent.Coinbase}\"}})");
                    });
                }
                break;
            default:
                // TODO: replace with a more suitable exception type. 
                throw new Exception($"Found {count} {Blockchains.Bitcoin.BitcoinChainAgent.Coinbase} nodes; expected zero or one.");
        }
    }
    private static async Task CreateIndexesAndConstraintsAsync(IDriver driver)
    {
        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));

        var heightLabel = PropertyMappingFactory.HeightProperty.Name;

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX ScriptAddressIndex " +
                $"IF NOT EXISTS " +
                $"FOR (n:{ScriptNodeStrategy.Label}) " +
                $"ON (n.{PropertyMappingFactory.Address<ScriptNode>(n => n.Address).Property.Name})");
        });

        var txidName = PropertyMappingFactory.TxId<TxNode>(n => n.Txid).Property.Name;
        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX TxidIndex " +
                $"IF NOT EXISTS " +
                $"FOR (n:{TxNodeStrategy.Label}) " +
                $"ON (n.{txidName})");
        });

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX BlockHeightIndex " +
                $"IF NOT EXISTS " +
                $"FOR (block:{BlockNodeStrategy.Label}) " +
                $"ON (block.{heightLabel})");
        });

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX GenerationEdgeIndex " +
                $"IF NOT EXISTS " +
                $"FOR ()-[r:{EdgeType.Mints}]->()" +
                $"on (r.{heightLabel})");
        });

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX TransferEdgeIndex " +
                $"IF NOT EXISTS " +
                $"FOR ()-[r:{EdgeType.Transfers}]->()" +
                $"on (r.{heightLabel})");
        });

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX FeeEdgeIndex " +
                $"IF NOT EXISTS " +
                $"FOR ()-[r:{EdgeType.Fee}]->()" +
                $"on (r.{heightLabel})");
        });
    }
}