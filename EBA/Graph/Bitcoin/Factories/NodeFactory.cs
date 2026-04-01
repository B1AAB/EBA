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
        out Model.INode createdNode,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? outHopsFromRoot = null)
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

    public static bool TryCreate<T>(List<IRecord> records, out List<T> nodes, string nodeVar = "n") where T: Model.INode
    {
        nodes = [];
        foreach (var record in records)
        {
            TryCreate(record[nodeVar].As<Neo4j.Driver.INode>(), out var createdNode);
            if (createdNode is not T)
                return false;

            nodes.Add((T)createdNode);
        }

        return true;
    }
}
