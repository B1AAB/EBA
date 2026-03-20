using EBA.Graph.Bitcoin.Strategies;
using INode = EBA.Graph.Model.INode;

namespace EBA.Graph.Bitcoin.Factories;

public class EdgeFactory
{
    public static IEdge<INode, INode> Create(
        INode source,
        INode target,
        IRelationship relationship)
    {
        return (source, target) switch
        {
            (CoinbaseNode, TxNode v) => C2TEdgeStrategy.Deserialize(v, relationship),
            (TxNode u, TxNode v) => T2TEdgeStrategy.Deserialize(u, v, relationship),
            (BlockNode u, TxNode v) => B2TEdgeStrategy.Deserialize(u, v, relationship),
            (TxNode u, ScriptNode v) => T2SEdgeStrategy.Deserialize(u, v, relationship),
            (ScriptNode u, TxNode v) => S2TEdgeStrategy.Deserialize(u, v, relationship),
            (BlockNode u, BlockNode v) => B2BEdgeStrategy.Deserialize(u, v, relationship),
            _ => throw new ArgumentException("Invalid edge type")
        };
    }
}
