using EBA.Utilities;
using INode = EBA.Graph.Model.INode;

namespace EBA.Blockchains.Bitcoin.Graph;

public class EdgeFactory
{
    public static IEdge<INode, INode> CreateEdge(
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
}
