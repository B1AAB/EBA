namespace EBA.Blockchains.Bitcoin.Graph;

public class CoinbaseNode : Node
{
    public new static GraphComponentType ComponentType { get { return GraphComponentType.BitcoinCoinbaseNode; } }
    public override GraphComponentType GetGraphComponentType() { return ComponentType; }

    public CoinbaseNode(Neo4j.Driver.INode node, double? originalOutdegree = null, double? hopsFromRoot = null) : base(node.ElementId, originalOutDegree: originalOutdegree, outHopsFromRoot: hopsFromRoot)
    { }

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
