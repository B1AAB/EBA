using EBA.Graph.Bitcoin;
using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

using Neo4j.Driver;

namespace EBA.Graph.Db.Neo4jDb;

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    private readonly Options _options;
    private readonly IDriver _driver;
    private readonly IStrategyFactory _strategyFactory;

    private List<Batch> _batches = [];

    private bool _disposed = false;

    public Neo4jDb(Options options, IStrategyFactory strategyFactory)
    {
        _options = options;
        _strategyFactory = strategyFactory;

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

            return await result.ToListAsync(cancellationToken: ct);
        });

        return rndRecords;
    }


    public async Task<List<IRecord>> GetNeighborsAsync(
        NodeLabels rootNodeLabel, 
        string rootNodeIdProperty,
        string rootNodeId,
        int queryLimit, 
        int maxLevel, 
        GraphTraversal traversalAlgorithm,
        CancellationToken ct,
        string relationshipFilter = "")
    {
        ct.ThrowIfCancellationRequested();

        var qBuilder = new StringBuilder();
        qBuilder.Append($"MATCH (root:{rootNodeLabel} {{ {rootNodeIdProperty}: \"{rootNodeId}\" }}) ");

        qBuilder.Append($"CALL apoc.path.spanningTree(root, {{");
        qBuilder.Append($"maxLevel: {maxLevel}, ");
        qBuilder.Append($"limit: {queryLimit}, ");

        if (traversalAlgorithm == GraphTraversal.BFS)
            qBuilder.Append($"bfs: true ");
        else if (traversalAlgorithm == GraphTraversal.DFS)
            qBuilder.Append($"bfs: false ");
        else
            throw new ArgumentException($"{traversalAlgorithm} is not supported, supported methods are {{ {GraphTraversal.DFS}, {GraphTraversal.BFS} }}");

        //qBuilder.Append($", labelFilter: '{labelFilters}'");

        if (!string.IsNullOrWhiteSpace(relationshipFilter))
            qBuilder.Append($", relationshipFilter: '{relationshipFilter}'");

        qBuilder.Append($"}}) ");
        qBuilder.Append($"YIELD path ");
        qBuilder.Append($"WITH root, ");
        qBuilder.Append($"nodes(path) AS pathNodes, ");
        qBuilder.Append($"relationships(path) AS pathRels ");
        qBuilder.Append($"LIMIT {queryLimit} ");
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
            return await result.ToListAsync(ct);
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

    public async Task SerializeConstantsAndConstraintsAsync(CancellationToken ct)
    {
        await _strategyFactory.SerializeConstantsAsync(_options.WorkingDir, ct);
        await _strategyFactory.SerializeSchemasAsync(_options.WorkingDir, ct);
    }

    public async Task SerializeAsync(T g,CancellationToken ct)
    {
        var nodes = g.GetNodes();
        var edges = g.GetEdges();
        var batchInfo = await GetBatchAsync([.. nodes.Keys, .. edges.Keys]);

        var tasks = new List<Task>();

        foreach (var type in nodes)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count(x => x.Id != NodeLabels.Coinbase.ToString()));
            var _strategy = _strategyFactory.GetStrategy(type.Key);
            tasks.Add(
                _strategy.ToCsvAsync(
                    type.Value.Where(x => x.Id != NodeLabels.Coinbase.ToString()),
                    batchInfo.GetFilename(type.Key)));
        }

        foreach (var type in edges)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count);
            var _strategy = _strategyFactory.GetStrategy(type.Key);
            tasks.Add(
                _strategy.ToCsvAsync(
                    type.Value,
                    batchInfo.GetFilename(type.Key)));
        }

        await Task.WhenAll(tasks);
    }

    private async Task<Batch> GetBatchAsync(List<GraphComponentType> types)
    {
        if (_batches.Count == 0)
            _batches = await DeserializeBatchesAsync();

        if (_batches.Count == 0 || _batches[^1].GetMaxCount() >= _options.Neo4j.MaxEntitiesPerBatch)
            _batches.Add(new Batch(_batches.Count.ToString(), _options.WorkingDir, types, _options.Neo4j.CompressOutput));

        return _batches[^1];
    }

    private async Task<List<Batch>> DeserializeBatchesAsync()
    {
        return await JsonSerializer<List<Batch>>.DeserializeAsync(
            _options.Neo4j.BatchesFilename);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _strategyFactory.Dispose();
            }

            _disposed = true;
        }
    }
}
