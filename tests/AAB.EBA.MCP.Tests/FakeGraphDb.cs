using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Graph.Model;
using AAB.EBA.GraphDb;
using AAB.EBA.GraphDb.Tests;
using Neo4j.Driver;
using DomainNode = AAB.EBA.Graph.Model.INode;
using INode = Neo4j.Driver.INode;

namespace AAB.EBA.MCP.Tests;

public class FakeGraphDb(GraphBase graph) : IGraphDb
{
    private readonly List<INode> _nodes = graph.Nodes.Select(ToFakeNode).ToList();
    private readonly List<IRelationship> _rels = graph.Edges.Select(ToFakeRelationship).ToList();

    public Task<IReadOnlyList<INode>> FindNodesAsync(
        NodeKind nodeKind,
        CancellationToken ct,
        string? orderByProperty = null,
        bool descending = false,
        int? limit = null)
    {
        var nodes = _nodes.Where(n => n.Labels.Contains(nodeKind.ToString()));

        if (!string.IsNullOrWhiteSpace(orderByProperty))
        {
            if (descending)
                nodes = nodes.OrderByDescending(
                    n => n.Properties.TryGetValue(
                        orderByProperty, out var val) ? val : null);
            else
                nodes = nodes.OrderBy(
                    n => n.Properties.TryGetValue(
                        orderByProperty, out var val) ? val : null);
        }

        if (limit is > 0)
            nodes = nodes.Take(limit.Value);

        return Task.FromResult<IReadOnlyList<INode>>(nodes.ToList());
    }

    public Task<List<IRelationship>> GetEdgesAsync(
        NodeKind nodeKind,
        string nodePropertyKey,
        string nodePropertyValue,
        CancellationToken ct,
        int? queryLimit = null)
    {
        var rootId = _nodes.FirstOrDefault(n =>
            n.Labels.Contains(nodeKind.ToString()) &&
            n.Properties.TryGetValue(nodePropertyKey, out var prop) &&
            prop?.ToString() == nodePropertyValue)?.ElementId;

        if (rootId == null)
            return Task.FromResult(new List<IRelationship>());

        var edges = _rels.Where(r => r.StartNodeElementId == rootId || r.EndNodeElementId == rootId);

        var result = queryLimit is > 0 ? edges.Take(queryLimit.Value).ToList() : edges.ToList();

        return Task.FromResult(result);
    }

    public Task<INode?> GetNodeAsync(NodeKind label, string propertyKey, string propertyValue, CancellationToken ct)
    {
        var node = _nodes.FirstOrDefault(n =>
            n.Labels.Contains(label.ToString()) &&
            n.Properties.TryGetValue(propertyKey, out var prop) &&
            prop?.ToString() == propertyValue);

        return Task.FromResult(node);
    }

    public Task VerifyConnectivityAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private static INode ToFakeNode(DomainNode domainNode)
    {
        return new FakeNeo4jNode
        {
            ElementId = domainNode.Id,
            Labels = new List<string> { domainNode.NodeKind.ToString() },
            Properties = GraphAdapter.GetNodeProperties(domainNode)
        };
    }

    private static IRelationship ToFakeRelationship(IEdge<DomainNode, DomainNode> domainEdge)
    {
        return new FakeNeo4jRelationship
        {
            ElementId = $"rel_{domainEdge.Source.Id}_{domainEdge.Relation}_{domainEdge.Target.Id}",
            StartNodeElementId = domainEdge.Source.Id,
            EndNodeElementId = domainEdge.Target.Id,
            Type = domainEdge.Relation.ToString(),
            Properties = GraphAdapter.GetEdgeProperties(domainEdge)
        };
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private class FakeNeo4jNode : INode
    {
        public long Id { get; init; }
        public string ElementId { get; init; } = string.Empty;
        public IReadOnlyList<string> Labels { get; init; } = [];
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();

        public object this[string key] => Properties[key];

        public T Get<T>(string key)
        {
            if (Properties.TryGetValue(key, out var value))
                return (T)value;

            throw new KeyNotFoundException($"The property '{key}' does not exist on the node.");
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (Properties.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Equals(IEntity? other) => other != null && ElementId == other.ElementId;
        public bool Equals(INode? other) => other != null && ElementId == other.ElementId;
    }

    private class FakeNeo4jRelationship : IRelationship
    {
        public long Id { get; init; }
        public string ElementId { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public long StartNodeId { get; init; }
        public string StartNodeElementId { get; init; } = string.Empty;
        public long EndNodeId { get; init; }
        public string EndNodeElementId { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();

        public object this[string key] => Properties[key];

        public T Get<T>(string key)
        {
            if (Properties.TryGetValue(key, out var value))
                return (T)value;

            throw new KeyNotFoundException($"The property '{key}' does not exist on the relationship.");
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (Properties.TryGetValue(key, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Equals(IEntity? other) => other != null && ElementId == other.ElementId;
        public bool Equals(IRelationship? other) => other != null && ElementId == other.ElementId;
    }
}