using EBA.Blockchains.Bitcoin.GraphModel;
using EBA.Graph.Bitcoin.Descriptors;
using EBA.Graph.Model;
using Neo4j.Driver;
using INode = EBA.Graph.Model.INode;

namespace AAB.EBA.GraphDb.Tests;

public class GraphAdapter
{
    public static async Task ToNeo4jAsync(IDriver driver, GraphBase graph)
    {
        await using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));

        await session.ExecuteWriteAsync(async tx =>
        {
            foreach (var node in graph.Nodes)
            {
                var nodeProps = GetNodeProperties(node);
                await tx.RunAsync(
                    $"MERGE " +
                    $"(n:`{node.NodeKind}` {{ `{node.GetIdPropertyName()}`: $id }}) " +
                    $"SET n += $props",
                    new { id = node.Id, props = nodeProps });
            }

            foreach (var edge in graph.Edges)
            {
                var edgeProps = GetEdgeProperties(edge);
                await tx.RunAsync(
                    $"MATCH " +
                    $"(s:`{edge.Source.NodeKind}` {{ `{edge.Source.GetIdPropertyName()}`: $sid }}), " +
                    $"(t:`{edge.Target.NodeKind}` {{ `{edge.Target.GetIdPropertyName()}`: $tid }}) " +
                    $"MERGE " +
                    $"(s)-[r:`{edge.EdgeKind}`]->(t) " +
                    $"SET r += $props",
                    new
                    {
                        sid = edge.Source.Id,
                        tid = edge.Target.Id,
                        props = edgeProps
                    });
            }
        });
    }

    public static Dictionary<string, object> GetNodeProperties(INode node)
    {
        var props = node switch
        {
            BlockNode b => BlockNodeDescriptor.StaticMapper.ToProperties(b),
            ScriptNode s => ScriptNodeDescriptor.StaticMapper.ToProperties(s),
            TxNode t => TxNodeDescriptor.StaticMapper.ToProperties(t),
            _ => throw new NotImplementedException($"Descriptor for {node.GetType().Name} not setup")
        };

        return FormatPropertiesForNeo4j(props);
    }

    public static Dictionary<string, object> GetEdgeProperties(IEdge<INode, INode> edge)
    {
        var props = edge switch
        {
            S2TEdge s2t => S2TEdgeDescriptor.StaticMapper.ToProperties(s2t),
            T2SEdge t2s => T2SEdgeDescriptor.StaticMapper.ToProperties(t2s),
            C2TEdge c2t => C2TEdgeDescriptor.StaticMapper.ToProperties(c2t),
            T2TEdge t2t => T2TEdgeDescriptor.StaticMapper.ToProperties(t2t),
            B2TEdge b2t => B2TEdgeDescriptor.StaticMapper.ToProperties(b2t),
            B2BEdge b2b => B2BEdgeDescriptor.StaticMapper.ToProperties(b2b),
            _ => throw new NotImplementedException($"Descriptor for {edge.GetType().Name} not setup")
        };

        return FormatPropertiesForNeo4j(props);
    }

    private static Dictionary<string, object> FormatPropertiesForNeo4j(Dictionary<string, object?> props)
    {
        var result = new Dictionary<string, object>();

        foreach (var (key, value) in props)
        {
            if (value is null) 
                continue;

            result[key] = value switch
            {
                Enum enumValue => enumValue.ToString(),
                ulong ulongValue => (long)ulongValue,
                uint uintValue => (long)uintValue,
                _ => value // keep as-is for other types (e.g., string, long, etc.)
            };
        }

        return result;
    }
}