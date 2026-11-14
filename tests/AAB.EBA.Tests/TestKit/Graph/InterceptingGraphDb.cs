using AAB.EBA.Graph.Db;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Graph.Model;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;

using Neo4j.Driver;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Graph;

/// <summary>
/// A decorator for <see cref="IGraphDb{T}"/> that forwards every call to an
/// inner database, and lets tests run code right before and right after a
/// graph is serialized. This is useful to observe, delay, or trigger other
/// actions (e.g., cancellation) at a known point of the serialization.
/// </summary>
public sealed class InterceptingGraphDb<T>(IGraphDb<T> inner) : IGraphDb<T>
    where T : GraphBase
{
    private readonly IGraphDb<T> _inner = inner;

    /// <summary>
    /// Invoked and awaited before the inner database serializes a graph.
    /// </summary>
    public Func<T, Task>? OnSerializingAsync { get; set; }

    /// <summary>
    /// Invoked and awaited after the inner database has serialized a graph.
    /// </summary>
    public Func<T, Task>? OnSerializedAsync { get; set; }

    public IStrategyFactory StrategyFactory => _inner.StrategyFactory;

    public async Task SerializeAsync(T graph, CancellationToken ct)
    {
        if (OnSerializingAsync is not null)
            await OnSerializingAsync(graph);

        await _inner.SerializeAsync(graph, ct);

        if (OnSerializedAsync is not null)
            await OnSerializedAsync(graph);
    }

    public Task VerifyConnectivityAsync(CancellationToken ct)
        => _inner.VerifyConnectivityAsync(ct);

    public Task SerializeConstantsAndConstraintsAsync(CancellationToken ct)
        => _inner.SerializeConstantsAndConstraintsAsync(ct);

    public Task ImportAsync(CancellationToken ct, string batchName = "")
        => _inner.ImportAsync(ct, batchName);

    public Task SampleAsync(CancellationToken ct)
        => _inner.SampleAsync(ct);

    public void ReportQueries()
        => _inner.ReportQueries();

    public Task BulkUpdateNodePropertiesAsync(
        NodeKind label,
        string idProperty,
        IReadOnlyList<Dictionary<string, object?>> updates,
        CancellationToken ct)
        => _inner.BulkUpdateNodePropertiesAsync(label, idProperty, updates, ct);

    public Task<List<IRecord>> GetRandomNodesAsync(
        NodeKind label,
        int count,
        CancellationToken ct,
        double rootNodeSelectProbability = 0.1,
        string nodeVariable = "randomNode")
        => _inner.GetRandomNodesAsync(label, count, ct, rootNodeSelectProbability, nodeVariable);

    public Task<List<IRecord>> GetNeighborhoodAsync(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int queryLimit,
        int maxLevel,
        bool useBFS,
        CancellationToken ct,
        string relationshipFilter = "")
        => _inner.GetNeighborhoodAsync(
            rootNodeLabel, rootNodeIdProperty, rootNodeId,
            queryLimit, maxLevel, useBFS, ct, relationshipFilter);

    public Task<NodeActivityCounts> GetNodeActivityCountsAsync(
        NodeKind label,
        string idProperty,
        string id,
        IReadOnlyList<BlockHeightRange> ranges,
        int scanLimit,
        CancellationToken ct)
        => _inner.GetNodeActivityCountsAsync(label, idProperty, id, ranges, scanLimit, ct);

    public Task<List<IRecord>> GetNodesAsync(
        NodeKind label,
        CancellationToken ct,
        string nodeVariable = "n",
        int? count = null)
        => _inner.GetNodesAsync(label, ct, nodeVariable, count);

    public Task<List<IRecord>> GetEdgesAsync(
        EdgeKind edgeKind,
        CancellationToken ct,
        string sourceNodeVariable = "source",
        string targetNodeVariable = "target",
        string relationshipVariable = "relationship",
        string? sourceNodeIdProperty = null,
        object? sourceNodeId = null,
        string? targetNodeIdProperty = null,
        object? targetNodeId = null)
        => _inner.GetEdgesAsync(
            edgeKind, ct, sourceNodeVariable, targetNodeVariable, relationshipVariable,
            sourceNodeIdProperty, sourceNodeId, targetNodeIdProperty, targetNodeId);

    public Task ExecuteWriteQueryAsync(List<string> schemas, CancellationToken ct)
        => _inner.ExecuteWriteQueryAsync(schemas, ct);

    public void Dispose()
        => _inner.Dispose();

    public ValueTask DisposeAsync()
        => _inner.DisposeAsync();
}
