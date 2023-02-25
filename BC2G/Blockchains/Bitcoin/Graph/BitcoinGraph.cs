namespace BC2G.Blockchains.Bitcoin.Graph;

// TODO: can the following AddOrUpdateEdge methods made generic and simplified?!

public class BitcoinGraph : GraphBase, IEquatable<BitcoinGraph>
{
    public void AddOrUpdateEdge(C2TEdge edge)
    {
        AddOrUpdateEdge(edge, (_, oldValue) => edge.Update(oldValue.Value));
    }

    public void AddOrUpdateEdge(T2TEdge edge)
    {
        AddOrUpdateEdge(edge, (_, oldEdge) => { return T2TEdge.Update((T2TEdge)oldEdge, edge); });
    }

    public void AddOrUpdateEdge(S2SEdge edge)
    {
        /// Note that the hashkey is invariant to the edge value.
        /// If this is changed, the `Equals` method needs to be
        /// updated accordingly.

        AddOrUpdateEdge(edge, (_, oldValue) => edge.Update(oldValue.Value));
    }

    public S2SEdge GetOrAddEdge(IRelationship e)
    {
        var source = GetOrAddNode(new ScriptNode(e.StartNodeElementId));
        var target = GetOrAddNode(new ScriptNode(e.EndNodeElementId));

        var edge = GetOrAddEdge(new S2SEdge(source, target, e));

        source.AddOutgoingEdges(edge);
        target.AddIncomingEdges(edge);

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
