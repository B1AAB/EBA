using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using Neo4j.Driver;

namespace AAB.EBA.GraphDb;

public interface IGraphDb : IDisposable, IAsyncDisposable
{
    public Task VerifyConnectivityAsync(CancellationToken ct);

    public Task<INode?> GetNodeAsync(
        NodeKind label,
        string propertyKey,
        object propertyValue,
        CancellationToken ct);

    public Task<IReadOnlyList<INode>> FindNodesAsync(
        NodeKind nodeKind,
        CancellationToken ct,
        string? orderByProperty = null,
        bool descending = false,
        int? limit = null);

    public Task<List<IRelationship>> GetEdgesAsync(
        NodeKind nodeKind,
        string nodePropertyKey,
        object nodePropertyValue,
        CancellationToken ct,
        int? queryLimit = null);

    public Task<List<IRecord>> GetNeighborsAsync(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int queryLimit,
        int maxLevel,
        bool useBFS,
        CancellationToken ct,
        string relationshipFilter = "");
}
