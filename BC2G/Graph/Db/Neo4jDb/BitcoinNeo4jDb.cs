using BC2G.Blockchains.Bitcoin.Graph;
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
            batchInfo.AddOrUpdate(type.Key, type.Value.Count(x => x.Id != BitcoinAgent.Coinbase));
            var _strategy = StrategyFactory.GetStrategy(type.Key);
            tasks.Add(
                _strategy.ToCsvAsync(
                    type.Value.Where(x => x.Id != BitcoinAgent.Coinbase), 
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
            var filename = Path.Join(Options.WorkingDir, $"cypher_query_{gcType}.txt");
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
    private static async Task CreateIndexesAndConstraintsAsync(IDriver driver)
    {
        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX ScriptAddressIndex " +
                $"IF NOT EXISTS " +
                $"FOR (n:{ScriptNodeStrategy.labels}) " +
                $"ON (n.{Props.ScriptAddress.Name})");
        });

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX TxidIndex " +
                $"IF NOT EXISTS " +
                $"FOR (n:{TxNodeStrategy.labels}) " +
                $"ON (n.{Props.Txid.Name})");
        });

        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX BlockHeightIndex " +
                $"IF NOT EXISTS " +
                $"FOR (block:{BlockGraphStrategy.labels}) " +
                $"ON (block.{Props.Height.Name})");
        });
        
        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX GenerationEdgeIndex " +
                $"IF NOT EXISTS " +
                $"FOR ()-[r:{EdgeType.Generation}]->()" +
                $"on (r.{Props.Height.Name})");
        });
        
        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX TransferEdgeIndex " +
                $"IF NOT EXISTS " +
                $"FOR ()-[r:{EdgeType.Transfer}]->()" +
                $"on (r.{Props.Height.Name})");
        });
        
        await session.ExecuteWriteAsync(async x =>
        {
            var result = await x.RunAsync(
                $"CREATE INDEX FeeEdgeIndex " +
                $"IF NOT EXISTS " +
                $"FOR ()-[r:{EdgeType.Fee}]->()" +
                $"on (r.{Props.Height.Name})");
        });
    }
}
