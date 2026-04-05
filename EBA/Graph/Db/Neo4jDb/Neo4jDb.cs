using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb;

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    private readonly Options _options;
    private readonly IDriver _driver;

    public IStrategyFactory StrategyFactory { get { return _strategyFactory; } }
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

    public async Task ExecuteWriteQueryAsync(List<string> queries, CancellationToken ct)
    {
        await VerifyConnectivityAsync(ct);

        await using var session = _driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Write));

        foreach (var query in queries)
        {
            ct.ThrowIfCancellationRequested();

            _logger.LogInformation("Executing write query: {schema}", query);

            var summary = await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query);
                return await cursor.ConsumeAsync();
            });

            _logger.LogInformation("Finished executing write query; {counters}", summary.Counters);
        }
    }

    public async Task SetRealizedCap(BlockNode blockNode, Dictionary<long, OHLCV> ohlcv, CancellationToken ct)
    {
        await VerifyConnectivityAsync(CancellationToken.None);
        await using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var readTx = await session.BeginTransactionAsync();

        var cursor = await readTx.RunAsync(
            $"MATCH (tx)-[r:{T2SEdge.Kind.Relation}]->(script) " +
            $"WHERE r.{nameof(T2SEdge.CreationHeight)} <= {blockNode.BlockMetadata.Height} " +
            $"AND r.{nameof(T2SEdge.SpentHeight)} > {blockNode.BlockMetadata.Height} " +
            $"AND script.{nameof(ScriptNode.ScriptType)} <> '{ScriptType.NullData}' " +
            $"RETURN r");

        decimal realizedCap = 0;

        while (await cursor.FetchAsync())
        {
            var r = cursor.Current["r"].As<IRelationship>();
            var creationHeight = (long)r.Properties[nameof(T2SEdge.CreationHeight)];
            var value = (long)r.Properties[nameof(T2SEdge.Value)];

            if (ohlcv.TryGetValue(creationHeight, out var blockOHLCV))
            {
                realizedCap += blockOHLCV.GetFiatValue(value);
            }
        }

        blockNode.BlockMetadata.RealizedCap = realizedCap;
    }

    private async Task<long> GetEdgeTypeCount(RelationType relationType, CancellationToken ct)
    {
        await using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        long edgeCount = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync($"MATCH ()-[r:{relationType}]->() RETURN count(r)");
            var record = await cursor.SingleAsync();
            return record[0].As<long>();
        });

        return edgeCount;
    }

    // TODO: this method should not be here;
    // it is specific to the Bitcoin graph model
    // and this class should be kept agnostic to the graph model.
    public async Task SetRealizedCap(
        SortedDictionary<long, BlockNode> blockNodes,
        Dictionary<long, OHLCV> ohlcv,
        CancellationToken ct)
    {
        if (blockNodes.Count == 0)
            return;

        var sortedHeights = blockNodes.Keys.ToArray();
        var minHeight = sortedHeights[0];
        var maxHeight = sortedHeights[^1];

        foreach (var block in blockNodes.Values)
            block.BlockMetadata.RealizedCap = 0;

        await VerifyConnectivityAsync(ct);
        await using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var edgeCounter = 0;
        var totalEdgeCount = await GetEdgeTypeCount(T2SEdge.Kind.Relation, ct);
        var processedEdgeCount = 0;
        var skippedEdgeCounter = 0;

        _logger.LogInformation("Starting iteration through {b} edges.", T2SEdge.Kind.Relation);

        await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(
                $"MATCH (tx)-[r:{T2SEdge.Kind.Relation}]->(script) " +
                $"WHERE " +
                $"r.{nameof(T2SEdge.CreationHeight)} <= {maxHeight} " +
                $"AND " +
                $"r.{nameof(T2SEdge.SpentHeight)} > {minHeight} " +
                $"AND " +
                $"script.{nameof(ScriptNode.ScriptType)} <> '{ScriptType.NullData}' " +
                $"RETURN " +
                $"r.{nameof(T2SEdge.CreationHeight)} AS creationHeight, " +
                $"r.{nameof(T2SEdge.SpentHeight)} AS spentHeight, " +
                $"r.{nameof(T2SEdge.Value)} AS value");

            while (await cursor.FetchAsync())
            {
                ct.ThrowIfCancellationRequested();
                edgeCounter++;

                var creationHeight = cursor.Current["creationHeight"].As<long>();
                var spentHeight = cursor.Current["spentHeight"].As<long>();
                var value = cursor.Current["value"].As<long>();

                if (!ohlcv.TryGetValue(creationHeight, out var blockOHLCV))
                {
                    skippedEdgeCounter++;
                }
                else
                {
                    var fiatValue = blockOHLCV.GetFiatValue(value);
                    foreach (var h in sortedHeights.GetViewBetween(creationHeight, spentHeight))
                        blockNodes[h].BlockMetadata.RealizedCap += fiatValue;

                    processedEdgeCount++;
                }

                if (edgeCounter % 100000 == 0)
                {
                    _logger.LogInformation(
                        "Traversed {e:n0} / {t:n0} edges. Processed {p:n0} edges and skipped {s:n0} edges due to missing related OHLCV data.",
                        edgeCounter, totalEdgeCount, processedEdgeCount, skippedEdgeCounter);
                }
            }
        });

        _logger.LogInformation(
            "Traversed {e:n0} / {t:n0} edges. Processed {p:n0} edges and skipped {s:n0} edges due to missing related OHLCV data.",
            edgeCounter, totalEdgeCount, processedEdgeCount, skippedEdgeCounter);
    }

    // TODO: this method should not be here;
    // it is specific to the Bitcoin graph model
    // and this class should be kept agnostic to the graph model.
    public async Task SetUTxOSpentHeight(CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting to find UTxO spending, and set their status to spent by setting {prop} on {r} edges.",
            nameof(S2TEdge.SpentHeight), T2SEdge.Kind.Relation);

        await VerifyConnectivityAsync(ct);

        var batch = new List<Dictionary<string, object>>();
        var batchIndex = 0;
        long totalProcessed = 0;

        await using var readSession = _driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Read));

        var readTx = await readSession.BeginTransactionAsync();

        // TODO: this should also report the total number of edges reported. 

        var cursor = await readTx.RunAsync(
            $"MATCH ()-[r:{S2TEdge.Kind.Relation}]->() " +
            $"WHERE r.{nameof(S2TEdge.Generated)} = false " +
            $"RETURN " +
            $"r.{nameof(S2TEdge.Txid)} AS txid, " +
            $"r.{nameof(S2TEdge.Vout)} AS vout, " +
            $"r.{nameof(S2TEdge.SpentHeight)} AS height");

        while (await cursor.FetchAsync())
        {
            ct.ThrowIfCancellationRequested();

            var record = cursor.Current;
            var height = record["height"].As<long>();

            batch.Add(new Dictionary<string, object>
            {
                ["txid"] = record["txid"].As<string>(),
                ["vout"] = record["vout"].As<int>(),
                ["height"] = height
            });

            if (batch.Count >= _options.Neo4j.MaxEntitiesPerBatch)
            {
                await CommitUTxOSpentHeight(batch, batchIndex++, ct);
                totalProcessed += batch.Count;
                batch = [];
            }
        }

        if (batch.Count > 0)
        {
            await CommitUTxOSpentHeight(batch, batchIndex++, ct);
            totalProcessed += batch.Count;
        }

        await readTx.CommitAsync();

        _logger.LogInformation(
            "Completed setting SpentHeight on Credits for {total:n0} Redeems edges.",
            totalProcessed);
    }

    private async Task CommitUTxOSpentHeight(
        IReadOnlyList<Dictionary<string, object>> batch,
        int batchIndex,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Committing batch {batch} with {count:n0} records.",
            batchIndex, batch.Count);

        await using var writeSession = _driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Write));

        var summary = await writeSession.ExecuteWriteAsync(async x =>
        {
            var cursor = await x.RunAsync(
                $"UNWIND $batch AS row " +
                $"MATCH (t:{TxNode.Kind} {{{nameof(TxNode.Txid)}: row.txid}})-[c:{T2SEdge.Kind.Relation}]->() " +
                $"WHERE c.{nameof(S2TEdge.Vout)} = row.vout " +
                $"SET c.{nameof(S2TEdge.SpentHeight)} = row.height",
                new Dictionary<string, object> { ["batch"] = batch });

            return await cursor.ConsumeAsync();
        });

        _logger.LogInformation(
            "Committing batch {batch} finished; set {props:n0} properties on {relation} edges.",
            batchIndex, summary.Counters.PropertiesSet, T2SEdge.Kind.Relation);
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

        var sampleProps = updates[0].Keys.Where(k => k != idProperty);
        var setClause = string.Join(
            ", ",
            sampleProps.Select(k => $"n.`{k}` = row.`{k}`"));

        // using toString() on the id property to match the string-typed
        // :ID column created by neo4j-admin import.
        var query =
            "UNWIND $batch AS row " +
            $"MATCH (n:{label} {{`{idProperty}`: toString(row.`{idProperty}`)}}) " +
            $"SET {setClause}";

        var batchIndex = 0;
        foreach (var batch in updates.Chunk(_options.Neo4j.MaxEntitiesPerBatch))
        {
            ct.ThrowIfCancellationRequested();

            await using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));
            var summary = await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    query,
                    new Dictionary<string, object> { ["batch"] = batch });

                return await cursor.ConsumeAsync();
            });

            var counters = summary.Counters;
            if (counters.PropertiesSet == 0)
                _logger.LogWarning("Batch {batch}: 0 properties set (MATCH may have found no nodes).",
                    batchIndex);
            else
                _logger.LogInformation("Batch {batch}: set {props:n0} properties on nodes.",
                    batchIndex, counters.PropertiesSet);

            batchIndex++;
        }

        _logger.LogInformation(
            "Completed bulk update of {total:n0} nodes with label {label}.",
            updates.Count, label);
    }
}
