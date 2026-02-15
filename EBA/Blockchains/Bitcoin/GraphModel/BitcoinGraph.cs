using INode = EBA.Graph.Model.INode;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class BitcoinGraph : GraphBase, IEquatable<BitcoinGraph>
{
    public INode GetOrAddNode(INode node)
    {
        return GetOrAddNode(node);
    }

    public IEdge<INode, INode> GetOrAddEdge(IRelationship e)
    {
        throw new NotImplementedException();
    }

    public IEdge<INode, INode> GetOrAddEdge(IRelationship e, INode sourceNode, INode targetNode)
    {
        var candidateEdge = EdgeFactory.CreateEdge(sourceNode, targetNode, e);

        if (TryGetOrAddEdge(candidateEdge, out var edge))
        {
            // edge was not in the graph, so it has been added,
            // hence the incoming/outgoing edges also need to be added.
            sourceNode.AddOutgoingEdge(edge);
            targetNode.AddIncomingEdge(edge);
        }

        return edge;
    }

    public bool Equals(BitcoinGraph? other)
    {
        if (other == null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        var otherNodes = other.Nodes;
        if (NodeCount != other.NodeCount)
            return false;

        if (EdgeCount != other.EdgeCount)
            return false;

        return Enumerable.SequenceEqual(
            Nodes.OrderBy(x => x),
            otherNodes.OrderBy(x => x));

        /*  var hashes = new HashSet<int>(_edges.Keys);
            foreach (var edge in otherEdges)
                /// Note that this hash method does not include
                /// edge value in the computation of hash key;
                /// this is in accordance with home with _edges.Keys
                /// are generated in the AddEdge method.
                if (!hashes.Remove(edge.GetHashCodeInt(true)))
                    return false;

            if (hashes.Count > 0)
                return false;

            return true;
        */
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BitcoinGraph);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
