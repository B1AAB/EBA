using AAB.EBA.GraphDb;
using AAB.EBA.GraphDb.Tests;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Graph.Model;
using Moq;
using Neo4j.Driver;
using INode = AAB.EBA.Graph.Model.INode;

namespace AAB.EBA.MCP.Tests;


public static class GraphMockSeeder
{
    public static Mock<IGraphDb> CreateMockDb(GraphBase graph)
    {
        // 1. Transform Domain models into Moq objects using LINQ
        var neo4jNodes = graph.Nodes.Select(CreateMockNode).ToList();
        var neo4jRels = graph.Edges.Select(CreateMockRel).ToList();

        var mockDb = new Mock<IGraphDb>();

        // 2. Wire up the read queries
        mockDb.Setup(db => db.GetNodeAsync(It.IsAny<NodeKind>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((NodeKind kind, string key, string value, CancellationToken ct) =>
                  neo4jNodes.FirstOrDefault(n =>
                      n.Labels.Contains(kind.ToString()) &&
                      n.Properties.TryGetValue(key, out var prop) &&
                      prop?.ToString() == value));

        mockDb.Setup(db => db.GetEdgesAsync(It.IsAny<NodeKind>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
              .ReturnsAsync((NodeKind kind, string key, string value, CancellationToken ct, int? limit) =>
              {
                  // Find the root node's ID first
                  var rootId = neo4jNodes.FirstOrDefault(n =>
                      n.Labels.Contains(kind.ToString()) &&
                      n.Properties.TryGetValue(key, out var prop) &&
                      prop?.ToString() == value)?.ElementId;

                  if (rootId == null) return []; // C# 12 empty list syntax

                  // Find connected edges
                  var edges = neo4jRels.Where(r => r.StartNodeElementId == rootId || r.EndNodeElementId == rootId);

                  return limit is > 0 ? edges.Take(limit.Value).ToList() : edges.ToList();
              });

        return mockDb;
    }

    private static Neo4j.Driver.INode CreateMockNode(INode domainNode)
    {
        var mock = new Mock<Neo4j.Driver.INode>();
        mock.SetupGet(n => n.ElementId).Returns(domainNode.Id);
        mock.SetupGet(n => n.Labels).Returns(new List<string> { domainNode.NodeKind.ToString() });
        mock.SetupGet(n => n.Properties).Returns(GraphAdapter.GetNodeProperties(domainNode));
        return mock.Object;
    }

    private static IRelationship CreateMockRel(IEdge<INode, INode> domainEdge)
    {
        var mock = new Mock<IRelationship>();
        mock.SetupGet(r => r.ElementId).Returns($"rel_{domainEdge.Source.Id}_{domainEdge.Relation}_{domainEdge.Target.Id}");
        mock.SetupGet(r => r.StartNodeElementId).Returns(domainEdge.Source.Id);
        mock.SetupGet(r => r.EndNodeElementId).Returns(domainEdge.Target.Id);
        mock.SetupGet(r => r.Type).Returns(domainEdge.Relation.ToString());
        mock.SetupGet(r => r.Properties).Returns(GraphAdapter.GetEdgeProperties(domainEdge));
        return mock.Object;
    }
}
