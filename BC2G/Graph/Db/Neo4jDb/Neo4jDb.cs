using BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

using Neo4j.Driver;

namespace BC2G.Graph.Db.Neo4jDb;

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    protected IDriver? Driver { private set; get; }
    protected Options Options { get; }

    /// <summary>
    /// Neo4j docs suggest between 10,000 and 100,000 updates 
    /// per transaction as a good target. 
    /// 
    /// Ref: https://neo4j.com/blog/bulk-data-import-neo4j-3-0/
    /// </summary>
    private const int _maxEntitiesPerBatch = 80000;
    private List<BatchInfo> _batches = new();

    private readonly IMapperFactory _mapperFactory;

    private readonly ILogger<Neo4jDb<T>> _logger;
    private bool _disposed = false;

    public Neo4jDb(Options options, ILogger<Neo4jDb<T>> logger, IMapperFactory mapperFactory)
    {
        Options = options;
        _logger = logger;
        _mapperFactory = mapperFactory;
    }

    /// <summary>
    /// No precedence should be assumed on serializing different types.
    /// </summary>
    public async Task SerializeAsync(T g, CancellationToken ct)
    {
        var edgeTypes = g.GetEdges();
        var graphType = Utilities.TypeToString(g.GetType());
        var batchInfo = await GetBatchAsync(edgeTypes.Keys.Append(graphType).ToList());

        var gMapper = _mapperFactory.GetGraphMapper(graphType);
        batchInfo.AddOrUpdate(graphType, 1);
        gMapper.ToCsv(g, batchInfo.GetFilename(graphType));

        foreach (var type in edgeTypes)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count);
            var eMapper = _mapperFactory.GetEdgeMapper(type.Key);
            eMapper.ToCsv(type.Value, batchInfo.GetFilename(type.Key));
        }

        await SerializeBatchesAsync();
    }

    /// <summary>
    /// No precedence should be assumed on serializing different types.
    /// </summary>
    public async Task ImportAsync(string batchName = "")
    {
        if (Driver is null)
            await SetupDriver(Options.Neo4j);

        _batches = await DeserializeBatchesAsync();
        IEnumerable<BatchInfo> batches;

        if (string.IsNullOrEmpty(batchName))
        {
            batches = _batches;
        }
        else
        {
            var batch = _batches.Find(x => x.Name == batchName);
            if (batch == default)
                throw new InvalidOperationException(
                    $"A batch named {batchName} not found in " +
                    $"{Options.Neo4j.BatchesFilename}");
            batches = new List<BatchInfo>() { batch };
        }

        foreach (var batch in batches)
        {
            foreach (var type in batch.TypesInfo)
            {
                _logger.LogInformation("Importing type `{t}` of batch `{b}`.", type.Key, batch.Name);
                var mapper = _mapperFactory.GetMapperBase(type.Key);
                await ExecuteQueryAsync(mapper, type.Value.Filename);
                _logger.LogInformation("Importing type `{t}` of batch `{b}` finished.", type.Key, batch.Name);
            }
        }
    }

    public async Task<bool> TrySampleAsync()
    {
        var sampledGraphsCounter = 0;
        var attempts = 0;
        var baseOutputDir = Path.Join(Options.WorkingDir, $"sampled_graphs_{Utilities.GetTimestamp()}");

        while (
            sampledGraphsCounter < Options.GraphSample.Count
            && ++attempts <= Options.GraphSample.MaxAttempts)
        {
            _logger.LogInformation(
                "Sampling {n} graphs; remaining {r}; attempt {a}/{m}.",
                Options.GraphSample.Count,
                Options.GraphSample.Count - sampledGraphsCounter,
                attempts, Options.GraphSample.MaxAttempts);

            var rndRootNodes = await GetRandomNodes(
                Options.GraphSample.Count - sampledGraphsCounter,
                Options.GraphSample.RootNodeSelectProb);

            foreach (var rootNode in rndRootNodes)
            {
                var baseDir = Path.Join(baseOutputDir, sampledGraphsCounter.ToString());
                if (await TrySampleNeighborsAsync(rootNode, baseDir))
                {
                    sampledGraphsCounter++;
                    _logger.LogInformation(
                        "Finished writting sampled graph {n}/{t} features to {b}.",
                        sampledGraphsCounter,
                        Options.GraphSample.Count,
                        baseDir);
                }
            }
        }

        if (attempts > Options.GraphSample.MaxAttempts)
        {
            _logger.LogError(
                "Failed creating {g} {g_msg} after {a} {a_msg}; created {c} {c_msg}. " +
                "You may retry, and if the error persists, try adjusting the values of " +
                "{minN}={minNV}, {maxN}={maxNV}, {minE}={minEV}, and {maxE}={maxEV}.",
                Options.GraphSample.Count,
                Options.GraphSample.Count > 1 ? "graphs" : "graph",
                attempts - 1,
                attempts > 1 ? "attempts" : "attempt",
                sampledGraphsCounter,
                sampledGraphsCounter > 1 ? "graphs" : "graph",
                nameof(Options.GraphSample.MinNodeCount), Options.GraphSample.MinNodeCount,
                nameof(Options.GraphSample.MaxNodeCount), Options.GraphSample.MaxNodeCount,
                nameof(Options.GraphSample.MinEdgeCount), Options.GraphSample.MinEdgeCount,
                nameof(Options.GraphSample.MaxEdgeCount), Options.GraphSample.MaxEdgeCount);
            return false;
        }
        else
        {
            return true;
        }
    }

    private async Task SetupDriver(Neo4jOptions options)
    {
        Driver = GraphDatabase.Driver(
            options.Uri,
            AuthTokens.Basic(options.User, options.Password));

        try
        {
            await Driver.VerifyConnectivityAsync();
        }
        catch (AggregateException)
        {
            throw;
        }

        await SetupAsync(Driver);
    }

    public virtual async Task SetupAsync(IDriver driver) { }

    private async Task ExecuteQueryAsync(IMapperBase mapper, string filename)
    {
        // Localization, if needed.
        // Neo4j import needs files to be placed in a particular folder 
        // before it can import them.
        var fileLocalized = false;
        var localFilename = filename;

        if (!Utilities.AssertPathEqual(
            Path.GetDirectoryName(filename),
            Options.Neo4j.ImportDirectory))
        {
            localFilename = Path.Join(Options.Neo4j.ImportDirectory, Path.GetFileName(filename));
            File.Copy(filename, localFilename, true);
            fileLocalized = true;
        }

        var filename4Query = Options.Neo4j.CypherImportPrefix + Path.GetFileName(localFilename);

        using var session = Driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));
        var queryResult = await session.ExecuteWriteAsync(async x =>
        {
            IResultCursor cursor = await x.RunAsync(mapper.GetQuery(filename4Query));
            return await cursor.ToListAsync();
        });

        // Delocalization.
        if (fileLocalized)
        {
            File.Delete(localFilename);
        }
    }

    private async Task<BatchInfo> GetBatchAsync(List<string> types)
    {
        if (_batches.Count == 0)
            _batches = await DeserializeBatchesAsync();

        if (_batches.Count == 0 || _batches[^1].GetTotalCount() >= _maxEntitiesPerBatch)
        {
            _batches.Add(new BatchInfo(
                (_batches.Count == 0 ? 0 : _batches.Count + 1).ToString(),
                Options.WorkingDir, types));
        }

        return _batches[^1];
    }
    private async Task SerializeBatchesAsync()
    {
        await JsonSerializer<List<BatchInfo>>.SerializeAsync(
            _batches, Options.Neo4j.BatchesFilename);
    }
    private async Task<List<BatchInfo>> DeserializeBatchesAsync()
    {
        return await JsonSerializer<List<BatchInfo>>.DeserializeAsync(
            Options.Neo4j.BatchesFilename);
    }

    private async Task<List<ScriptNode>> GetRandomNodes(
        int nodesCount, double rootNodesSelectProb = 0.1)
    {
        using var session = Driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Read));

        var rndNodeVar = "rndScript";
        var rndRecords = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH ({rndNodeVar}:{ScriptMapper.labels})-[:{EdgeType.Transfer}]->() " +
                $"WHERE rand() < {rootNodesSelectProb} " +
                $"RETURN {rndNodeVar} LIMIT {nodesCount}");

            return await result.ToListAsync();
        });

        var rndNodes = new List<ScriptNode>();
        foreach (var n in rndRecords)
            rndNodes.Add(new ScriptNode(n.Values[rndNodeVar].As<Neo4j.Driver.INode>()));

        return rndNodes;
    }

    private async Task<bool> TrySampleNeighborsAsync(ScriptNode rootNode, string baseOutputDir)
    {
        var graph = await GetNeighborsAsync(rootNode.Address, Options.GraphSample.Hops);

        if (!CanUseGraph(
            graph, tolerance: 0,
            minNodeCount: Options.GraphSample.MinNodeCount,
            maxNodeCount: Options.GraphSample.MaxNodeCount,
            minEdgeCount: Options.GraphSample.MinEdgeCount,
            maxEdgeCount: Options.GraphSample.MaxEdgeCount))
            return false;


        if (Options.GraphSample.Mode == GraphSampleMode.GraphRndEdgePair)
        {
            var rndGraph = await GetRandomEdges(graph.EdgeCount);

            if (!CanUseGraph(
                rndGraph,
                minNodeCount: Options.GraphSample.MinNodeCount,
                maxNodeCount: graph.GetNodeCount<ScriptNode>(),
                minEdgeCount: Options.GraphSample.MinEdgeCount,
                maxEdgeCount: graph.EdgeCount))
                return false;

            rndGraph.AddLabel(1);
            rndGraph.GetFeatures(Path.Join(baseOutputDir, "random_edges"));
        }

        graph.AddLabel(0);
        graph.GetFeatures(Path.Join(baseOutputDir, "graph"));


        return true;
    }

    private static bool CanUseGraph(
        GraphBase g,
        int minNodeCount = 3, int maxNodeCount = 200,
        int minEdgeCount = 3, int maxEdgeCount = 200,
        double tolerance = 0.5)
    {
        // TODO: implement checks on the graph; e.g., graph size, or if it was already defined.

        // TODO: very big graphs cause various issues
        // with Tensorflow when training, such as out-of-memory
        // (hence radically slow process), or even trying to
        // multiply matrixes of very large size 2**32 or even
        // larger. There should be much better workarounds at
        // Tensorflow level, but for now, we limit the size of graphs.
        if (g.NodeCount <= minNodeCount - (minNodeCount * tolerance) ||
            g.NodeCount >= maxNodeCount + (maxNodeCount * tolerance) ||
            g.EdgeCount <= minEdgeCount - (minEdgeCount * tolerance) ||
            g.EdgeCount >= maxEdgeCount + (maxEdgeCount * tolerance))
            return false;

        return true;
    }

    private async Task<GraphBase> GetNeighborsAsync(
        string rootScriptAddress, int maxHops)
    {
        using var session = Driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Read));

        var samplingResult = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH path = (p: {ScriptMapper.labels} {{ Address: \"{rootScriptAddress}\"}}) -[:{EdgeType.Transfer} * 1..{maxHops}]->(p2: {ScriptMapper.labels}) " +
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

        var g = new GraphBase();

        foreach (var hop in samplingResult)
        {
            var root = new ScriptNode(hop.Values["root"].As<List<Neo4j.Driver.INode>>()[0]);
            if (root is null)
                continue;

            g.GetOrAddNode(root);

            // It is better to add nodes like this, and not just as part of 
            // adding edge, because `nodes` has all the node properties for each 
            // node, but `relationships` only contain their IDs.
            foreach (var node in hop.Values["nodes"].As<List<Neo4j.Driver.INode>>())
                g.GetOrAddNode(new ScriptNode(node));

            foreach (var relationship in hop.Values["relationships"].As<List<IRelationship>>())
                g.GetOrAddEdge(relationship);
        }

        return g;
    }

    private async Task<GraphBase> GetRandomEdges(
        int edgeCount, double edgeSelectProb = 0.2)
    {
        using var session = Driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Read));

        var rndNodesResult = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"Match (source:{ScriptMapper.labels})-[edge:{EdgeType.Transfer}]->(target:{ScriptMapper.labels}) " +
                $"where rand() < {edgeSelectProb} " +
                $"return source, edge, target limit {edgeCount}");

            return await result.ToListAsync();
        });

        var g = new GraphBase();
        foreach (var n in rndNodesResult)
        {
            g.GetOrAddNode(new ScriptNode(n.Values["source"].As<Neo4j.Driver.INode>()));
            g.GetOrAddNode(new ScriptNode(n.Values["target"].As<Neo4j.Driver.INode>()));
            g.GetOrAddEdge(n.Values["edge"].As<IRelationship>());
        }
        return g;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing) { }
        }

        _disposed = true;
    }
}
