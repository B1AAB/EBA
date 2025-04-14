using BC2G.Utilities;

namespace BC2G.Graph.Model;

public class EdgeFactory
{
    public static IEdge<INode, INode> CreateEdge(
        INode source,
        INode target,
        IRelationship relationship,
        GraphComponentType sourceNodeGraphComponentType,
        GraphComponentType targetNodeGraphComponentType)
    {
        //var id = relationship.ElementId;
        var value = Helpers.BTC2Satoshi((double)relationship.Properties[Props.EdgeValue.Name]);
        var type = Enum.Parse<EdgeType>(relationship.Type);
        var blockHeight = (long)relationship.Properties[Props.Height.Name];
        uint timestamp = 0; // TODO currently edges stored on the database do not have a timestamp

        if (sourceNodeGraphComponentType == GraphComponentType.BitcoinCoinbaseNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinTxNode)
        {
            return new C2TEdge((TxNode)target, value, timestamp, blockHeight);
        }
        else if (
            sourceNodeGraphComponentType == GraphComponentType.BitcoinCoinbaseNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinScriptNode)
        {
            return new C2SEdge((ScriptNode)target, value, timestamp, blockHeight);
        }
        else if (
            sourceNodeGraphComponentType == GraphComponentType.BitcoinTxNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinTxNode)
        {
            return new T2TEdge((TxNode)source, (TxNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            sourceNodeGraphComponentType == GraphComponentType.BitcoinScriptNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinScriptNode)
        {
            return new S2SEdge((ScriptNode)source, (ScriptNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            sourceNodeGraphComponentType == GraphComponentType.BitcoinScriptNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinBlockNode)
        {
            return new S2BEdge((ScriptNode)source, (BlockNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            sourceNodeGraphComponentType == GraphComponentType.BitcoinBlockNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinScriptNode)
        {
            return new B2SEdge((BlockNode)source, (ScriptNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            sourceNodeGraphComponentType == GraphComponentType.BitcoinTxNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinBlockNode)
        {
            return new T2BEdge((TxNode)source, (BlockNode)target, value, type, timestamp, blockHeight);
        }
        else if (
            sourceNodeGraphComponentType == GraphComponentType.BitcoinBlockNode &&
            targetNodeGraphComponentType == GraphComponentType.BitcoinTxNode)
        {
            return new B2TEdge((BlockNode)source, (TxNode)target, value, type, timestamp, blockHeight);
        }
        else
        {
            throw new ArgumentException("Invalid edge type");
        }
    }
}
