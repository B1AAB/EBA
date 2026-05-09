using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Graph.Model;
using AAB.EBA.GraphDb;
using AAB.EBA.GraphDb.Tests;
using Neo4j.Driver;
using DomainNode = AAB.EBA.Graph.Model.INode;
using INode = Neo4j.Driver.INode;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

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

    public Task<INode?> GetNodeAsync(
        NodeKind label,
        string propertyKey,
        object propertyValue,
        CancellationToken ct)
    {
        var node = _nodes.FirstOrDefault(n =>
            n.Labels.Contains(label.ToString()) &&
            n.Properties.TryGetValue(propertyKey, out var prop) &&
            Equals(prop, propertyValue));

        return Task.FromResult(node);
    }

    public Task<List<IRelationship>> GetEdgesAsync(
        NodeKind nodeKind,
        string nodePropertyKey,
        object nodePropertyValue,
        CancellationToken ct,
        int? queryLimit = null)
    {
        var rootId = _nodes.FirstOrDefault(n =>
            n.Labels.Contains(nodeKind.ToString()) &&
            n.Properties.TryGetValue(nodePropertyKey, out var prop) &&
            Equals(prop, nodePropertyValue))?.ElementId;

        if (rootId == null)
            return Task.FromResult(new List<IRelationship>());

        var edges = _rels.Where(r => r.StartNodeElementId == rootId || r.EndNodeElementId == rootId);

        var result = queryLimit is > 0 ? edges.Take(queryLimit.Value).ToList() : edges.ToList();

        return Task.FromResult(result);
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

    public Task<List<IRecord>> GetNeighborsAsync(
        NodeKind rootNodeLabel,
        string rootNodeIdProperty,
        string rootNodeId,
        int queryLimit,
        int maxLevel,
        bool useBFS,
        CancellationToken ct,
        string relationshipFilter = "")
    {
        throw new NotImplementedException("GetNeighborsAsync method not implemented.");
        /*
        var rootNode = _nodes.FirstOrDefault(n =>
            n.Labels.Contains(rootNodeLabel.ToString()) &&
            n.Properties.TryGetValue(rootNodeIdProperty, out var prop) &&
            Equals(prop, rootNodeId));

        if (rootNode == null)
            return Task.FromResult(new List<IRecord>());

        // Start with the root, then expand level by level up to maxLevel.
        var visitedIds = new List<string> { rootNode.ElementId };
        var currentLevelNodes = new List<INode> { rootNode };
        var neighborNodes = new List<INode>();
        var collectedRels = new List<IRelationship>();

        for (int level = 0; level < maxLevel; level++)
        {
            var nextLevelNodes = new List<INode>();

            foreach (var current in currentLevelNodes)
            {
                foreach (var edge in _rels)
                {
                    bool startsHere = edge.StartNodeElementId == current.ElementId;
                    bool endsHere = edge.EndNodeElementId == current.ElementId;
                    if (!startsHere && !endsHere) continue;

                    if (!string.IsNullOrWhiteSpace(relationshipFilter) && edge.Type != relationshipFilter)
                        continue;

                    var neighborId = startsHere ? edge.EndNodeElementId : edge.StartNodeElementId;
                    if (visitedIds.Contains(neighborId)) continue;

                    var neighbor = _nodes.FirstOrDefault(n => n.ElementId == neighborId);
                    if (neighbor == null) continue;

                    visitedIds.Add(neighborId);
                    neighborNodes.Add(neighbor);
                    collectedRels.Add(edge);
                    nextLevelNodes.Add(neighbor);

                    if (neighborNodes.Count >= queryLimit) break;
                }
                if (neighborNodes.Count >= queryLimit) break;
            }

            if (neighborNodes.Count >= queryLimit) break;
            currentLevelNodes = nextLevelNodes;
        }

        // Build a single record matching the shape consumers expect.
        var rootDict = new List<object>
        {
            new Dictionary<string, object>
            {
                ["node"] = rootNode,
                ["inDegree"] = (long)_rels.Count(r => r.EndNodeElementId == rootNode.ElementId),
                ["outDegree"] = (long)_rels.Count(r => r.StartNodeElementId == rootNode.ElementId)
            }
        };

        var nodeDicts = new List<object>();
        foreach (var n in neighborNodes)
        {
            nodeDicts.Add(new Dictionary<string, object>
            {
                ["node"] = n,
                ["inDegree"] = (long)_rels.Count(r => r.EndNodeElementId == n.ElementId),
                ["outDegree"] = (long)_rels.Count(r => r.StartNodeElementId == n.ElementId)
            });
        }

        var record = new FakeNeo4jRecord(new Dictionary<string, object>
        {
            ["root"] = rootDict,
            ["nodes"] = nodeDicts,
            ["relationships"] = collectedRels
        });

        // Consumer reads index 0 for "root" and indices >= 1 for "nodes"/"relationships",
        // so include the same record twice.
        return Task.FromResult(new List<IRecord> { record, record });*/
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

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