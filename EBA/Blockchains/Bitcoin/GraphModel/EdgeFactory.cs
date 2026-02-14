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

        if (source.GetGraphComponentType() == GraphComponentType.BitcoinCoinbaseNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinTxNode)
        {
            return new C2TEdge((TxNode)target, value, timestamp, blockHeight);
        }
        else if (
            source.GetGraphComponentType() == GraphComponentType.BitcoinTxNode &&
            target.GetGraphComponentType() == GraphComponentType.BitcoinTxNode)
        {
            return new T2TEdge((TxNode)source, (TxNode)target, value, type, timestamp, blockHeight);
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
            var createdInBlockHeight = (long)relationship.Properties[nameof(S2TEdge.UTxOCreatedInBlockHeight)];
            return new S2TEdge((ScriptNode)source, (TxNode)target, value, type, timestamp, blockHeight, createdInBlockHeight);
        }
        else
        {
            throw new ArgumentException("Invalid edge type");
        }
    }
}
