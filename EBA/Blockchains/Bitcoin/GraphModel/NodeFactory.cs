using EBA.Graph.Bitcoin.Strategies;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class NodeFactory
{
    /// <summary>
    /// Attempts to create a strongly-typed graph node instance from the given Neo4j node
    /// </summary>
    /// <returns> true if the node was successfully converted 
    /// to a strongly-typed graph node; otherwise, false.</returns>
    public static bool TryCreateNode(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double outHopsFromRoot, 
        out EBA.Graph.Model.INode createdNode)
    {
        if (node.Labels.Contains(ScriptNodeStrategy.Label.ToString()))
        {
            createdNode = ScriptNodeStrategy.Deserialize(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);

            return true;
        }
        else if (node.Labels.Contains(TxNodeStrategy.Label.ToString()))
        {
            createdNode = TxNodeStrategy.Deserialize(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);

            return !((TxNode)createdNode).HasNullFeatures();
        }
        else if (node.Labels.Contains(BlockNodeStrategy.Label.ToString()))
        {
            createdNode = BlockNodeStrategy.Deserialize(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);

            return true;
        }
        else if (node.Labels.Contains(BitcoinChainAgent.Coinbase.ToString()))
        {
            createdNode = new CoinbaseNode(
                node,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);

            return true;
        }
        else
        {
            throw new NotImplementedException(
                $"Unexpected node type, labels: {string.Join(',', node.Labels)}");
        }
    }
}
