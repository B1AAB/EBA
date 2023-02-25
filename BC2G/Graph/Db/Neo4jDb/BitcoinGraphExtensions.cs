namespace BC2G.Graph.Db.Neo4jDb;

internal static class BitcoinGraphExtensions
{
    public static S2SEdge GetOrAddEdge(this GraphBase g, IRelationship e)
    {
        var source = g.GetOrAddNode(new ScriptNode(e.StartNodeElementId));
        var target = g.GetOrAddNode(new ScriptNode(e.EndNodeElementId));

        var edge = g.GetOrAddEdge(new S2SEdge(source, target, e));

        source.AddOutgoingEdges(edge);
        target.AddIncomingEdges(edge);

        return edge;
    }
}
