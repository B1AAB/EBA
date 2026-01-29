namespace EBA.Blockchains.Bitcoin.GraphModel;

public class CoinbaseNode : Node
{
    public new static GraphComponentType ComponentType { get { return GraphComponentType.BitcoinCoinbaseNode; } }
    public override GraphComponentType GetGraphComponentType() { return ComponentType; }

    public CoinbaseNode(
        Neo4j.Driver.INode node,
        double? originalOutdegree = null,
        double? hopsFromRoot = null) : 
        base("Coinbase",
            originalInDegree: 0,
            originalOutDegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: node.ElementId)
    { }

    public override string GetIdPropertyName()
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
