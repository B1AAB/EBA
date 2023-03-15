using BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

namespace BC2G.Graph.Db.Neo4jDb;

public class BitcoinNeo4jDb : Neo4jDb<BitcoinGraph>
{
    public BitcoinNeo4jDb(Options options, ILogger<BitcoinNeo4jDb> logger) :
        base(options, logger, new BitcoinStrategyFactory())
    { }

    public override async Task<IDriver> GetDriver(Neo4jOptions options)
    {
        var driver = await base.GetDriver(options);
        await EnsureCoinbaseNodeAsync(driver);
        await EnsureConstraintsAsync(driver);

        return driver;
    }

    public override async Task SerializeAsync(BitcoinGraph g, CancellationToken ct)
    {
        var nodes = g.GetNodes();
        var edges = g.GetEdges();
        var graphType = BitcoinGraph.ComponentType;
        var batchInfo = await GetBatchAsync(
            nodes.Keys.Concat(edges.Keys).Append(graphType).ToList());

        batchInfo.AddOrUpdate(graphType, 1);
        var graphStrategy = StrategyFactory.GetStrategy(graphType);
        await graphStrategy.ToCsvAsync(g, batchInfo.GetFilename(graphType));

        foreach (var type in nodes)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count(x => x.Id != BitcoinAgent.Coinbase));
            var _strategy = StrategyFactory.GetStrategy(type.Key);
            await _strategy.ToCsvAsync(type.Value.Where(x => x.Id != BitcoinAgent.Coinbase), batchInfo.GetFilename(type.Key));
        }

        foreach (var type in edges)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count);
            var _strategy = StrategyFactory.GetStrategy(type.Key);
            await _strategy.ToCsvAsync(type.Value, batchInfo.GetFilename(type.Key));
        }
    }

    public override Task ImportAsync(string batchName = "", List<GraphComponentType>? importOrder = null)
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
        return base.ImportAsync(batchName, importOrder);
    }

    public override async Task<List<ScriptNode>> GetRandomNodes(
        IDriver driver, int nodesCount, double rootNodesSelectProb = 0.1)
    {
        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var rndNodeVar = "rndScript";
        var rndRecords = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH ({rndNodeVar}:{ScriptNodeStrategy.labels})-[:{EdgeType.Transfer}]->() " +
                $"WHERE rand() < {rootNodesSelectProb} " +
                $"RETURN {rndNodeVar} LIMIT {nodesCount}");

            return await result.ToListAsync();
        });

        var rndNodes = new List<ScriptNode>();
        foreach (var n in rndRecords)
            rndNodes.Add(new ScriptNode(n.Values[rndNodeVar].As<Neo4j.Driver.INode>()));

        return rndNodes;
    }

    public override async Task<bool> TrySampleNeighborsAsync(
        IDriver driver, ScriptNode rootNode, string baseOutputDir)
    {
        var graph = await GetNeighborsAsync(driver, rootNode.Address, Options.GraphSample.Hops);

        if (!CanUseGraph(
            graph, tolerance: 0,
            minNodeCount: Options.GraphSample.MinNodeCount,
            maxNodeCount: Options.GraphSample.MaxNodeCount,
            minEdgeCount: Options.GraphSample.MinEdgeCount,
            maxEdgeCount: Options.GraphSample.MaxEdgeCount))
            return false;

        if (Options.GraphSample.Mode == GraphSampleMode.GraphRndEdgePair)
        {
            var rndGraph = await GetRandomEdges(driver, graph.EdgeCount);

            if (!CanUseGraph(
                rndGraph,
                minNodeCount: Options.GraphSample.MinNodeCount,
                maxNodeCount: graph.GetNodeCount(GraphComponentType.BitcoinScriptNode),
                minEdgeCount: Options.GraphSample.MinEdgeCount,
                maxEdgeCount: graph.EdgeCount))
                return false;

            rndGraph.AddLabel(1);
            rndGraph.WriteFeatures(Path.Join(baseOutputDir, "random_edges"));
        }

        graph.AddLabel(0);
        graph.WriteFeatures(Path.Join(baseOutputDir, "graph"));

        return true;
    }

    public override async Task<GraphBase> GetNeighborsAsync(
        IDriver driver, string rootScriptAddress, int maxHops)
    {
        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var samplingResult = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH path = (p: {ScriptNodeStrategy.labels} {{ Address: \"{rootScriptAddress}\"}}) -[:{EdgeType.Transfer} * 1..{maxHops}]->(p2: {ScriptNodeStrategy.labels}) " +
                "WITH p, [n in nodes(path) where n <> p | n] as nodes, relationships(path) as relationships " +
                "WITH collect(distinct p) as root, size(nodes) as cnt, collect(nodes[-1]) as nodes, collect(distinct relationships[-1]) as relationships " +
                "RETURN root, nodes, relationships");

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

        var g = new BitcoinGraph();

        foreach (var hop in samplingResult)
        {
            var root = new ScriptNode(hop.Values["root"].As<List<Neo4j.Driver.INode>>()[0]);
            if (root is null)
                continue;

            g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, root);

            // It is better to add nodes like this, and not just as part of 
            // adding edge, because `nodes` has all the node properties for each 
            // node, but `relationships` only contain their IDs.
            foreach (var node in hop.Values["nodes"].As<List<Neo4j.Driver.INode>>())
                g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, new ScriptNode(node));

            foreach (var relationship in hop.Values["relationships"].As<List<IRelationship>>())
                g.GetOrAddEdge(relationship);
        }

        return g;
    }

    public override async Task<GraphBase> GetRandomEdges(
        IDriver driver, int edgeCount, double edgeSelectProb = 0.2)
    {
        using var session = driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Read));

        var randomNodes = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"Match (source:{ScriptNodeStrategy.labels})-[edge:{EdgeType.Transfer}]->(target:{ScriptNodeStrategy.labels}) " +
                $"where rand() < {edgeSelectProb} " +
                $"return source, edge, target limit {edgeCount}");

            return await result.ToListAsync();
        });

        var g = new BitcoinGraph();

        foreach (var n in randomNodes)
        {
            g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, new ScriptNode(n.Values["source"].As<Neo4j.Driver.INode>()));
            g.GetOrAddNode(GraphComponentType.BitcoinScriptNode, new ScriptNode(n.Values["target"].As<Neo4j.Driver.INode>()));
            g.GetOrAddEdge(n.Values["edge"].As<IRelationship>());
        }
        return g;
    }

    private static async Task EnsureCoinbaseNodeAsync(IDriver driver)
    {
        int count = 0;
        using (var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read)))
        {
            count = await session.ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync($"MATCH (n:{BitcoinAgent.Coinbase}) RETURN COUNT(n)");
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
                        await tx.RunAsync(
                            $"CREATE (:{BitcoinAgent.Coinbase} {{" +
                            $"{Props.ScriptAddress.Name}: " +
                            $"\"{BitcoinAgent.Coinbase}\"}})");
                    });
                }
                break;
            default:
                // TODO: replace with a more suitable exception type. 
                throw new Exception($"Found {count} {BitcoinAgent.Coinbase} nodes; expected zero or one.");
        }
    }
    private static async Task EnsureConstraintsAsync(IDriver driver)
    {
        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));

        /* TODO:
         * - create constraints on address and script type so lookup could be much faster. 
         * -- check if constraints already exist or not, and add if missing. 
         * 
         * - When using MERGE or MATCH with LOAD CSV, make sure you have an index or a 
         * unique constraint on the property that you are merging on. This will 
         * ensure that the query executes in a performant way.
         * 
         * - make sure apoc and all the necessary plug-ins are installed on the given
         * Neo4j database at the time of initialization. Currently if any of the plug 
         * ins are not installed it fails at the load time. 
         */

        // TODO: do not create contraints if they already exist,
        // otherwise you'll get the following error: 
        //
        // One or more errors occurred. (An equivalent constraint
        // already exists, 'Constraint( id=4,
        // name='UniqueAddressContraint', type='UNIQUENESS',
        // schema=(:Script {Address}), ownedIndex=3 )'.)
        //
        // Solution: first check if the constraint exists, and only 
        // create if not.
        //
        // Check if existing contraints as the following, and add contrains only
        // if they are not already defined. Alternatively, try creating the contrains, 
        // and if they already exist, you'll see an Exception (non-blocking) in the
        // above code. 
        /*var xyz = await session.ReadTransactionAsync(async x =>
        {
            var result = await x.RunAsync("CALL db.constraints");
            return result.ToListAsync();
        });*/

        // TODO: handle the exceptions raised in running the following.
        // Note that the exceptions are stored in the Exceptions property
        // and do not log and stop execution when raised. 

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    "CREATE CONSTRAINT UniqueScriptAddressContraint " +
                    $"FOR (script:{ScriptNodeStrategy.labels}) " +
                    $"REQUIRE script.{Props.ScriptAddress.Name} IS UNIQUE");
            });
        }
        catch (Exception) { }

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"CREATE INDEX FOR (script:{ScriptNodeStrategy.labels}) " +
                    $"ON (script.{Props.ScriptAddress.Name})");
            });
        }
        catch (Exception)
        {

        }


        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    "CREATE CONSTRAINT UniqueTxidContraint " +
                    $"FOR (n:{TxNodeStrategy.labels}) " +
                    $"REQUIRE n.{Props.Txid.Name} IS UNIQUE");
            });
        }
        catch (Exception e)
        {

        }

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"CREATE INDEX FOR (n:{TxNodeStrategy.labels})" +
                    $" on (n.{Props.Txid.Name})");
            });
        }
        catch (Exception e)
        {

        }

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    "CREATE CONSTRAINT UniqueBlockHeightContraint " +
                    $"FOR (n:{BlockGraphStrategy.labels}) " +
                    $"REQUIRE n.{Props.Height.Name} IS UNIQUE");
            });
        }
        catch (Exception e)
        {

        }

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"CREATE INDEX FOR (block:{BlockGraphStrategy.labels})" +
                    $" on (block.{Props.Height.Name})");
            });
        }
        catch (Exception e)
        {

        }
    }
}
