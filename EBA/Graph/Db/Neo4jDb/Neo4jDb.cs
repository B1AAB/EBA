namespace EBA.Graph.Db.Neo4jDb;

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    private readonly Options _options;
    private readonly IDriver _driver;
    private readonly IStrategyFactory _strategyFactory;

    private List<Batch> _batches = [];

    private bool _disposed = false;
    private readonly ILogger<Neo4jDb<T>> _logger;

    public Neo4jDb(Options options, ILogger<Neo4jDb<T>> logger, IStrategyFactory strategyFactory)
    {
        _options = options;
        _logger = logger;
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
        NodeKind label,
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

    public async Task<List<IRecord>> GetNodesAsync(
        NodeKind label, 
        CancellationToken ct, 
        string nodeVariable = "n",
        int? count=null)
    {
        await VerifyConnectivityAsync(ct);

        using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var rndRecords = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(
                $"MATCH ({nodeVariable}:{label}) " +
                (count.HasValue ? $"LIMIT {count.Value} " : "") +
                $"RETURN {nodeVariable}");

            return await result.ToListAsync(cancellationToken: ct);
        });

        return rndRecords;
    }

    public async Task<List<IRecord>> GetNeighborsAsync(
        NodeKind rootNodeLabel, 
        string rootNodeIdProperty,
        string rootNodeId,
        int queryLimit, 
        int maxLevel, 
        bool useBFS,
        CancellationToken ct,
        string relationshipFilter = "")
    {
        ct.ThrowIfCancellationRequested();

        var qBuilder = new StringBuilder();
        qBuilder.Append($"MATCH (root:{rootNodeLabel} {{ {rootNodeIdProperty}: \"{rootNodeId}\" }}) ");

        qBuilder.Append($"CALL apoc.path.spanningTree(root, {{");
        qBuilder.Append($"maxLevel: {maxLevel}, ");
        qBuilder.Append($"limit: {queryLimit}, ");

        if (useBFS)
            qBuilder.Append($"bfs: true ");
        else 
            qBuilder.Append($"bfs: false ");

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

    public Task ImportAsync(CancellationToken ct, string batchName = "")
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
        var batchInfo = await GetBatchAsync();

        var tasks = new List<Task>();

        foreach (var nodeGroup in nodes.Where(x => _strategyFactory.IsSerializable(x.Key)))
        {
            batchInfo.Update(nodeGroup.Key, nodeGroup.Value.Count);
            var _strategy = _strategyFactory.GetStrategy(nodeGroup.Key);
            if (_strategy == null) 
                continue;

            tasks.Add(_strategy.ToCsvAsync(nodeGroup.Value, batchInfo.GetFilename(nodeGroup.Key)));
        }

        foreach (var edgeGroup in edges.Where(x => _strategyFactory.IsSerializable(x.Key)))
        {
            batchInfo.Update(edgeGroup.Key, edgeGroup.Value.Count);
            var _strategy = _strategyFactory.GetStrategy(edgeGroup.Key);
            if (_strategy == null)
                continue;

            tasks.Add(_strategy.ToCsvAsync(edgeGroup.Value, batchInfo.GetFilename(edgeGroup.Key)));
        }

        await Task.WhenAll(tasks);
    }

    private async Task<Batch> GetBatchAsync()
    {
        if (_batches.Count == 0)
            _batches = await DeserializeBatchesAsync();


        if (_batches.Count == 0 || _batches[^1].GetMaxCount() >= _options.Neo4j.MaxEntitiesPerBatch)
            _batches.Add(new Batch(
                _batches.Count.ToString(),
                _options.WorkingDir,
                _strategyFactory.NodeStrategies,
                _strategyFactory.EdgeStrategies,
                _options.Neo4j.CompressOutput));

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

    public async Task BulkUpdateNodePropertiesAsync(
        NodeKind label,
        string idProperty,
        IReadOnlyList<Dictionary<string, object?>> updates,
        CancellationToken ct)
    {
        if (updates.Count == 0)
            return;

        await VerifyConnectivityAsync(ct);

        var setClause = string.Join(", ", updates[0].Keys
            .Where(k => k != idProperty)
            .Select(k => $"n.{k} = row.{k}"));

        var innerQuery =
            $"MATCH (n:{label} {{{idProperty}: row.{idProperty}}}) " +
            $"SET {setClause}";

        var chunkIndex = 0;
        foreach (var chunk in updates.Chunk(_options.Neo4j.MaxEntitiesPerBatch))
        {
            ct.ThrowIfCancellationRequested();

            // Wrap the inner query in apoc.periodic.iterate for
            // server-side batching and parallel execution.
            // Ref: https://neo4j.com/blog/nodes/nodes-2019-best-practices-to-make-large-updates-in-neo4j
            var apocQuery =
                "CALL apoc.periodic.iterate(" +
                "'UNWIND $batch AS row RETURN row', " +
                $"'{innerQuery}', " +
                "{batchSize: 5000, parallel: true, params: {batch: $batch}}" +
                ") YIELD batches, total, timeTaken, errorMessages " +
                "RETURN batches, total, timeTaken, errorMessages";

            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));
            var cursor = await session.RunAsync(apocQuery, new Dictionary<string, object>
            {
                ["batch"] = chunk
            });

            var record = await cursor.SingleAsync();
            var total = record["total"].As<long>();
            var timeTaken = record["timeTaken"].As<long>();
            var errorMessages = record["errorMessages"].As<IDictionary<string, object>>();
            
            if (errorMessages.Count > 0)
                _logger.LogError("Chunk {chunk}: {total:n0} nodes in {time}s with errors: {errors}",
                    chunkIndex, total, timeTaken, string.Join("; ", errorMessages));
            else
                _logger.LogInformation("Chunk {chunk}: updated {total:n0} nodes in {time}s.",
                    chunkIndex, total, timeTaken);

            chunkIndex++;
        }

        _logger.LogInformation(
            "Completed bulk update of {total:n0} nodes with label {label}.",
            updates.Count, label);
    }
}
