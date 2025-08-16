namespace BC2G.Blockchains.Bitcoin.Graph;

public class CoinbaseNode(Neo4j.Driver.INode node) : CoinbaseNode<ContextBase>(node, new ContextBase())
{ }

public class CoinbaseNode<T>(Neo4j.Driver.INode node, T context) : Node<T>(node.ElementId, context)
    where T : IContext
{
    public new static GraphComponentType ComponentType { get { return GraphComponentType.BitcoinCoinbaseNode; } }
    public override GraphComponentType GetGraphComponentType() { return ComponentType; }

    public override string GetUniqueLabel()
    {
        return "Coinbase";
    }

    public static new string[] GetFeaturesName()
    {
        return ["Coinbase"];
    }

    public override string[] GetFeatures()
    {
        return ["0"];
    }
}
