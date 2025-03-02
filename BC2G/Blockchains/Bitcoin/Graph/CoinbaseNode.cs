namespace BC2G.Blockchains.Bitcoin.Graph;

public class CoinbaseNode : Node
{
    public new static GraphComponentType ComponentType { get { return GraphComponentType.BitcoinCoinbaseNode; } }
    public override GraphComponentType GetGraphComponentType() { return ComponentType; }

    public CoinbaseNode(Neo4j.Driver.INode node) : base(node.ElementId)
    { }

    public override string GetUniqueLabel()
    {
        return "Coinbase";
    }

    public static new string[] GetFeaturesName()
    {
        return ["Coinbase"];
    }

    public override double[] GetFeatures()
    {
        return [0];
    }
}
