using EBA.Graph.Bitcoin.Strategies;
using EBA.Utilities;
using INode = EBA.Graph.Model.INode;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class EdgeFactory
{
    public static IEdge<INode, INode> CreateEdge(
        INode source,
        INode target,
        IRelationship relationship)
    {
        var value = Helpers.BTC2Satoshi(PropertyMappingFactory.ValueBTC<IRelationship>(null!).Deserialize<double>(relationship.Properties));
        var type = Enum.Parse<EdgeType>(relationship.Type);
        var blockHeight = PropertyMappingFactory.Height<IRelationship>(null!).Deserialize<long>(relationship.Properties);
        uint timestamp = 0; // TODO currently edges stored on the database do not have a timestamp

        return (source, target) switch
        {
            (CoinbaseNode, TxNode v) => new C2TEdge(v, value, timestamp, blockHeight),
            (TxNode u, TxNode v) => new T2TEdge(u, v, value, type, timestamp, blockHeight),
            (TxNode u, BlockNode v) => new T2BEdge(u, v, value, type, timestamp, blockHeight),
            (BlockNode u, TxNode v) => new B2TEdge(u, v, value, type, timestamp, blockHeight),
            (TxNode u, ScriptNode v) => new T2SEdge(u, v, value, type, timestamp, blockHeight),
            (ScriptNode u, TxNode v) => new S2TEdge(u, v, value, type, timestamp, blockHeight, (long)relationship.Properties[nameof(S2TEdge.UTxOCreatedInBlockHeight)]),
            _ => throw new ArgumentException("Invalid edge type")
        };
    }
}
