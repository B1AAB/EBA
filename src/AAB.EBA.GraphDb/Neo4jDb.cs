using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System.Text;

namespace AAB.EBA.GraphDb;

public class Neo4jDb : IGraphDb
{
    private readonly Options _options;
    private readonly IDriver _driver;

    private bool _disposed = false;
    private readonly ILogger<Neo4jDb> _logger;

    public Neo4jDb(Options options, ILogger<Neo4jDb> logger)
    {
        _options = options;
        _logger = logger;

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

    public async Task<INode?> GetNodeAsync(
        NodeKind label,
        string propertyKey,
        object propertyValue,
        CancellationToken ct)
    {
        await VerifyConnectivityAsync(ct);

        using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        var nodeVar = "n";

        // do not use interpolated strings for property values,
        // use parameter placeholder ($propValue) instead,
        // because otherwise it can treat all values as strings,
        // which will fail to match if the value of property is not a string in the database.
        var q = 
            $"MATCH ({nodeVar}:{label} " +
            $"{{ {propertyKey}: $propValue }}) " +
            $"RETURN {nodeVar}";

        _logger.LogDebug("Executing query: {q} with property {prop}", q, propertyValue);
        
        var records = await session.ExecuteReadAsync(async x =>
        {
            var result = await x.RunAsync(q, new { propValue = propertyValue });
            return await result.ToListAsync(cancellationToken: ct);
        });

        if (records.Count == 0)
            return null;

        if (records.Count > 1)
            throw new InvalidOperationException(
                $"Multiple nodes found with '{propertyKey}'='{propertyValue}'.");

        if (!records[0].ContainsKey(nodeVar))
            throw new InvalidOperationException(
                $"Returned record does not contain expected node variable '{nodeVar}'.");

        return records[0][nodeVar].As<INode>();
    }

    public async Task<List<IRelationship>> GetEdgesAsync(
        NodeKind nodeKind,
        string nodePropertyKey,
        object nodePropertyValue,
        CancellationToken ct,
        int? queryLimit = null)
    {
        await VerifyConnectivityAsync(ct);

        // do not use interpolated strings for property values,
        // use parameter placeholder ($propValue) instead,
        // because otherwise it can treat all values as strings,
        // which will fail to match if the value of property is not a string in the database.
        var q =
            $"MATCH (n:{nodeKind} {{ `{nodePropertyKey}`: $propValue }})-[r]-() " +
            $"RETURN r";

        if (queryLimit != null && queryLimit.Value > 0)
            q += $" LIMIT {queryLimit.Value}";

        using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));

        _logger.LogDebug("Executing query: {Query} with property {prop}", q, nodePropertyValue);

        var records = await session.ExecuteReadAsync(async x =>
        {
            var cursor = await x.RunAsync(q, new { propValue = nodePropertyValue });
            return await cursor.ToListAsync(cancellationToken: ct);
        });

        return [.. records.Select(record => record["r"].As<IRelationship>())];
    }

    public async Task<IReadOnlyList<INode>> FindNodesAsync(
        NodeKind nodeKind, 
        CancellationToken ct, 
        string? orderByProperty = null, 
        bool descending = false, 
        int? limit = null)
    {
        var qBuilder = new StringBuilder($"MATCH (n:{nodeKind}) RETURN n ");

        if (!string.IsNullOrWhiteSpace(orderByProperty))
        {
            var direction = descending ? "DESC" : "ASC";
            qBuilder.Append($"ORDER BY n.`{orderByProperty}` {direction} ");
        }

        if (limit.HasValue)
            qBuilder.Append($"LIMIT {limit.Value}");

        await VerifyConnectivityAsync(ct);
        await using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));
        var cursor = await session.RunAsync(qBuilder.ToString());

        var nodes = new List<INode>();
        await foreach (var record in cursor)
            nodes.Add(record["n"].As<INode>());

        return nodes;
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
            { }

            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
