namespace BC2G.Blockchains.Bitcoin.Graph;

// TODO: can the following AddOrUpdateEdge methods made generic and simplified?!

public class BitcoinGraph : GraphBase, IEquatable<BitcoinGraph>
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinGraph; }
    }

    public void AddOrUpdateEdge(C2TEdge edge)
    {
        if (edge.Value == 0)
            return;

        AddOrUpdateEdge(
            edge, 
            (_, oldValue) => edge.Update(oldValue.Value),
            TxNode.ComponentType,
            TxNode.ComponentType,
            C2TEdge.ComponentType);
    }

    public void AddOrUpdateEdge(C2SEdge edge)
    {
        if (edge.Value == 0)
            return;

        AddOrUpdateEdge(
            edge,
            (_, oldValue) => edge.Update(oldValue.Value),
            ScriptNode.ComponentType,
            ScriptNode.ComponentType,
            C2SEdge.ComponentType);
    }

    public void AddOrUpdateEdge(T2TEdge edge)
    {
        if (edge.Value == 0)
            return;

        AddOrUpdateEdge(
            edge,
            (_, oldEdge) => { return T2TEdge.Update((T2TEdge)oldEdge, edge); },
            TxNode.ComponentType,
            TxNode.ComponentType,
            T2TEdge.ComponentType);
    }

    public void AddOrUpdateEdge(S2SEdge edge)
    {
        /// Note that the hashkey is invariant to the edge value.
        /// If this is changed, the `Equals` method needs to be
        /// updated accordingly.

        if (edge.Value == 0)
            return;

        AddOrUpdateEdge(
            edge,
            (_, oldValue) => edge.Update(oldValue.Value),
            ScriptNode.ComponentType,
            ScriptNode.ComponentType,
            S2SEdge.ComponentType);
    }

    public S2SEdge GetOrAddEdge(IRelationship e)
    {
        var source = GetOrAddNode(GraphComponentType.BitcoinScriptNode, new ScriptNode(e.StartNodeElementId));
        var target = GetOrAddNode(GraphComponentType.BitcoinScriptNode, new ScriptNode(e.EndNodeElementId));

        var edge = GetOrAddEdge(GraphComponentType.BitcoinS2S, new S2SEdge(source, target, e));

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
