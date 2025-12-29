using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;
using EBA.Utilities;
using INode = EBA.Graph.Model.INode;

namespace EBA.Blockchains.Bitcoin.Graph;

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

    public void AddOrUpdateEdge(S2TEdge edge)
    {
        if (edge.Value == 0)
            return;

        AddOrUpdateEdge(
            edge,
            (_, oldValue) => edge.Update(oldValue.Value),
            ScriptNode.ComponentType,
            TxNode.ComponentType,
            S2TEdge.ComponentType);
    }

    public void AddOrUpdateEdge(T2SEdge edge)
    {
        if (edge.Value == 0)
            return;

        AddOrUpdateEdge(
            edge,
            (_, oldValue) => edge.Update(oldValue.Value),
            TxNode.ComponentType,
            ScriptNode.ComponentType,
            T2SEdge.ComponentType);
    }

    public void AddOrUpdateEdge(B2TEdge edge)
    {
        if (edge.Value == 0)
            return;

        AddOrUpdateEdge(
            edge,
            (_, oldValue) => edge.Update(oldValue.Value),
            BlockNode.ComponentType,
            TxNode.ComponentType,
            B2TEdge.ComponentType);
    }


    public static INode NodeFactory(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double outHopsFromRoot)
    {
        if (node.Labels.Contains(ScriptNodeStrategy.Label.ToString()))
        {
            return new ScriptNode(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                outHopsFromRoot: outHopsFromRoot);
        }
        else if (node.Labels.Contains(TxNodeStrategy.Label.ToString()))
        {
            return TxNode.CreateTxNode(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);
        }
        else if (node.Labels.Contains(BlockNodeStrategy.Label.ToString()))
        {
            return new BlockNode(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                outHopsFromRoot: outHopsFromRoot);
        }
        else if (node.Labels.Contains(BitcoinChainAgent.Coinbase.ToString()))
        {
            return new CoinbaseNode(
                node,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);
        }
        else
        {
            throw new NotImplementedException(
                $"Unexpected node type, labels: {string.Join(',', node.Labels)}");
        }
    }

    public static IEdge<INode, INode> EdgeFactory(
        INode source,
        INode target,
        IRelationship relationship)
    {
        //var id = relationship.ElementId;
        var value = Helpers.BTC2Satoshi((double)relationship.Properties[Props.EdgeValue.Name]);
        var type = Enum.Parse<EdgeType>(relationship.Type);
        var blockHeight = (long)relationship.Properties[Props.Height.Name];
        uint timestamp = 0; // TODO currently edges stored on the database do not have a timestamp

        if (source.GetGraphComponentType() == GraphComponentType.BitcoinCoinbaseNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinTxNode)
        {
            return new C2TEdge((TxNode)target, value, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinCoinbaseNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode)
        {
            return new C2SEdge((ScriptNode)target, value, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinTxNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinTxNode)
        {
            return new T2TEdge((TxNode)source, (TxNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode)
        {
            return new S2SEdge((ScriptNode)source, (ScriptNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinBlockNode)
        {
            return new S2BEdge((ScriptNode)source, (BlockNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinBlockNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode)
        {
            return new B2SEdge((BlockNode)source, (ScriptNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinTxNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinBlockNode)
        {
            return new T2BEdge((TxNode)source, (BlockNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinBlockNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinTxNode)
        {
            return new B2TEdge((BlockNode)source, (TxNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinTxNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode)
        {
            return new T2SEdge((TxNode)source, (ScriptNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinScriptNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinTxNode)
        {
            return new S2TEdge((ScriptNode)source, (TxNode)target, value, type, timestamp, blockHeight);
        }
        else
        {
            throw new ArgumentException("Invalid edge type");
        }
    }

    public INode GetOrAddNode(INode node)
    {
        return GetOrAddNode(node.GetGraphComponentType(), node);
    }

    public IEdge<INode, INode> GetOrAddEdge(IRelationship e)
    {
        throw new NotImplementedException();
    }

    public IEdge<INode, INode> GetOrAddEdge(IRelationship e, INode sourceNode, INode targetNode)
    {
        var candidateEdge = EdgeFactory(sourceNode, targetNode, e);

        var wasAdded = TryGetOrAddEdge(candidateEdge.GetGraphComponentType(), candidateEdge, out var edge);

        if (wasAdded)
        {
            // TODO 2: it does not seems the source and target nodes are the correct instances
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
