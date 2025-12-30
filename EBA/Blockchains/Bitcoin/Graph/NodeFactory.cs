using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

namespace EBA.Blockchains.Bitcoin.Graph;

public class NodeFactory
{
    public static EBA.Graph.Model.INode CreateNode(
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
}
