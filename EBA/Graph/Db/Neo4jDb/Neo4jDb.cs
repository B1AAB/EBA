using EBA.Graph.Bitcoin;
using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

using Neo4j.Driver;

namespace EBA.Graph.Db.Neo4jDb;

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    private readonly Options _options;
    private readonly IDriver _driver;

    public Neo4jDb(Options options)
    {
        _options = options;

        // As per suggestions at https://neo4j.com/blog/developer/neo4j-driver-best-practices
        // reuse a driver rather than initializing a new instance per request.
        _driver = GraphDatabase.Driver(
            _options.Neo4j.Uri,
            AuthTokens.Basic(_options.Neo4j.User, _options.Neo4j.Password));
    }

    public async Task VerifyConnectivityAsync(CancellationToken ct)
    {
        try
        {
            await _driver.VerifyConnectivityAsync();
        }
        catch (AggregateException)
        {
            throw;
        }
    }

    public async Task<List<IRecord>> GetRandomNodesAsync(
        NodeLabels label,
        int count,
        CancellationToken ct,
        double rootNodeSelectProbability = 0.1,
        string nodeVariable = "randomNode")
    {
        await VerifyConnectivityAsync(ct);

        using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var rndRecords = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH ({nodeVariable}:{label}) " +
                $"WHERE rand() < {rootNodeSelectProbability} " +
                $"WITH {nodeVariable} " +
                $"ORDER BY rand() " +
                $"LIMIT {count} " +
                $"RETURN {nodeVariable}");

            return await result.ToListAsync();
        });

        return rndRecords;
    }


    public async Task<List<IRecord>> GetNeighborsAsync(
        NodeLabels rootNodeLabel, 
        string rootNodePropKey, 
        string rootNodePropValue, 
        int queryLimit, 
        string labelFilters, 
        int maxLevel, 
        GraphTraversal traversalAlgorithm,
        string relationshipFilter = "")
    {
        var qBuilder = new StringBuilder();
        if (rootNodeLabel == NodeLabels.Coinbase)
            qBuilder.Append($"MATCH (root:{NodeLabels.Coinbase.ToString()}) ");
        else
            qBuilder.Append($"MATCH (root:{rootNodeLabel} {{ {rootNodePropKey}: \"{rootNodePropValue}\" }}) ");

        qBuilder.Append($"CALL apoc.path.spanningTree(root, {{");
        qBuilder.Append($"maxLevel: {maxLevel}, ");
        qBuilder.Append($"limit: {queryLimit}, ");

        if (traversalAlgorithm == GraphTraversal.BFS)
            qBuilder.Append($"bfs: true, ");
        else if (traversalAlgorithm == GraphTraversal.DFS)
            qBuilder.Append($"bfs: false, ");
        else
            throw new ArgumentException($"{traversalAlgorithm} is not supported, supported methods are {{ {GraphTraversal.DFS}, {GraphTraversal.BFS} }}");

        qBuilder.Append($"labelFilter: '{labelFilters}'");

        if (!string.IsNullOrWhiteSpace(relationshipFilter))
            qBuilder.Append($", relationshipFilter: '{relationshipFilter}'");

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

        using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));
        var samplingResult = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(qBuilder.ToString());
            return await result.ToListAsync();
        });

        return samplingResult;
    }

    public Task ImportAsync(CancellationToken ct, string batchName = "", List<GraphComponentType>? importOrder = null)
    {
        throw new NotImplementedException();
    }

    public void ReportQueries()
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
}
