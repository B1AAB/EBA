using BC2G.Graph.Db.Neo4jDb.Bitcoin.Strategies;

namespace BC2G.Graph.Db.Neo4jDb;

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    private readonly ILogger<Neo4jDb<T>> _logger;
    private readonly Options _options;
    private readonly IDriver _driver;

    public Neo4jDb(Options options, ILogger<Neo4jDb<T>> logger)
    {
        _logger = logger;
        _options = options;

        try
        {
            _driver = GraphDatabase.Driver(
                _options.Neo4j.Uri,
                AuthTokens.Basic(_options.Neo4j.User, _options.Neo4j.Password));

            _driver.VerifyConnectivityAsync().Wait();

            _logger.LogDebug("Connected to Neo4j database.");
        }
        catch (AggregateException)
        {
            _logger.LogError("Failed connecting to Neo4j database.");
            throw;
        }
    }

    public void ReportQueries()
    {
        throw new NotImplementedException();
    }

    public Task ImportAsync(CancellationToken ct, string batchName = "", List<GraphComponentType>? importOrder = null)
    {
        throw new NotImplementedException();
    }

    public Task SampleAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task SerializeAsync(T graph, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public async Task<List<Model.INode>> GetRandomNodes(
        string nodeType, int count, double nodeSelectProbability = 0.1)
    {
        if (nodeType != ScriptNodeStrategy.Labels)
            throw new NotImplementedException("Currently only ScriptNode is supported.");

        using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var rndNodeVar = "x";
        var rndRecords = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH ({rndNodeVar}:{nodeType}) " +
                $"WHERE rand() < {nodeSelectProbability} " +
                $"WITH {rndNodeVar} " +
                $"ORDER BY rand() " +
                $"LIMIT {count} " +
                $"RETURN {rndNodeVar}");

            return await result.ToListAsync();
        });

        var rndNodes = new List<Model.INode>();
        foreach (var n in rndRecords)
            if (nodeType == ScriptNodeStrategy.Labels) // TODO: this should be a factory.
                rndNodes.Add(new ScriptNode(n.Values[rndNodeVar].As<Neo4j.Driver.INode>()));

        return rndNodes;
    }

    public async Task<List<IRecord>> GetNeighbors(
        string rootNodeLabel,
        string propKey,
        string propValue,
        int queryLimit,
        string labelFilters,
        int maxLevel,
        SamplingAlgorithm traversalAlgorithm)
    {
        var builder = new StringBuilder();
        builder.Append($"MATCH (root:{rootNodeLabel} {{ {propKey}: \"{propValue}\" }}) ");

        builder.Append($"CALL apoc.path.spanningTree(root, {{");
        builder.Append($"maxLevel: {maxLevel}, ");
        builder.Append($"limit: {queryLimit}, ");

        switch (traversalAlgorithm)
        {
            case SamplingAlgorithm.BFS:
                builder.Append($"bfs: true, ");
                break;
            case SamplingAlgorithm.DFS:
                builder.Append($"bfs: false, ");
                break;
            default:
                throw new NotImplementedException();
        }

        builder.Append($"labelFilter: '{labelFilters}'");
        //$"    relationshipFilter: \">{EdgeType.Transfers}\"" +
        builder.Append($"}}) ");
        builder.Append($"YIELD path ");
        builder.Append($"WITH root, ");
        builder.Append($"nodes(path) AS pathNodes, ");
        builder.Append($"relationships(path) AS pathRels ");
        builder.Append($"LIMIT {queryLimit} ");
        //qBuilder.Append($"RETURN [root] AS root, [n IN pathNodes WHERE n <> root] AS nodes, pathRels AS relationships");
        // ******** 
        builder.Append($"RETURN ");
        builder.Append($"[ {{");
        builder.Append($"node: root, ");
        builder.Append($"inDegree: COUNT {{ (root)<--() }}, ");
        builder.Append($"outDegree: COUNT {{ (root)-->() }} ");
        builder.Append($"}}] AS root, ");
        builder.Append($"[ ");
        builder.Append($"n IN pathNodes WHERE n <> root ");
        builder.Append($"| ");
        builder.Append($"{{ ");
        builder.Append($"node: n, ");
        builder.Append($"inDegree: COUNT {{ (n)<--() }}, ");
        builder.Append($"outDegree: COUNT {{ (n)-->() }} ");
        builder.Append($"}} ");
        builder.Append($"] AS nodes, ");
        builder.Append($"pathRels AS relationships");

        var query = builder.ToString();

        using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        return await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(query);
            return await result.ToListAsync();
        });
    }
}
