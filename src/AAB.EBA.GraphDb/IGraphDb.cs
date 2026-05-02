using EBA.Blockchains.Bitcoin.GraphModel;
using Neo4j.Driver;

namespace AAB.EBA.GraphDb;

public interface IGraphDb : IDisposable, IAsyncDisposable
{
    public Task VerifyConnectivityAsync(CancellationToken ct);

    public Task<INode?> GetNodeAsync(
        NodeKind label,
        string propertyKey,
        string propertyValue,
        CancellationToken ct);

    public Task<List<IRelationship>> GetEdgesAsync(
        NodeKind nodeKind,
        string nodePropertyKey,
        string nodePropertyValue,
        CancellationToken ct,
        int? queryLimit = null);
}
