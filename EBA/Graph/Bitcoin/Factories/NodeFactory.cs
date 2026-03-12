using EBA.Graph.Bitcoin.Strategies;

namespace EBA.Graph.Bitcoin.Factories;

public class NodeFactory
{
    /// <summary>
    /// Attempts to create a strongly-typed graph node instance from the given Neo4j node
    /// </summary>
    /// <returns> true if the node was successfully converted 
    /// to a strongly-typed graph node; otherwise, false.</returns>
    public static bool TryCreate(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double outHopsFromRoot, 
        out Model.INode createdNode)
    {
        if (node.Labels.Contains(ScriptNode.Kind.ToString()))
        {
            createdNode = ScriptNodeStrategy.Deserialize(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);

            return true;
        }
        else if (node.Labels.Contains(TxNode.Kind.ToString()))
        {
            createdNode = TxNodeStrategy.Deserialize(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);

            return !((TxNode)createdNode).HasNullFeatures();
        }
        else if (node.Labels.Contains(BlockNode.Kind.ToString()))
        {
            createdNode = BlockNodeStrategy.Deserialize(
                node,
                originalIndegree: originalIndegree,
                originalOutdegree: originalOutdegree,
                hopsFromRoot: outHopsFromRoot);

            return true;
        }
        else if (node.Labels.Contains(CoinbaseNode.Kind.ToString()))
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
