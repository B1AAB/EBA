namespace EBA.Blockchains.Bitcoin.GraphModel;

public class CoinbaseNode(
    Neo4j.Driver.INode node,
    double? originalOutdegree = null,
    double? hopsFromRoot = null) 
    : Node(
        id: Kind.ToString(),
        originalInDegree: 0,
        originalOutDegree: originalOutdegree,
        outHopsFromRoot: hopsFromRoot,
        idInGraphDb: node.ElementId)
{
    public new static NodeKind Kind => NodeKind.Coinbase;
    public override NodeKind NodeKind => Kind;

    public override string GetIdPropertyName()
    {
        return Kind.ToString();
    }

    public static new string[] GetFeaturesName()
    {
        return [Kind.ToString()];
    }

    public override string[] GetFeatures()
    {
        return ["0"];
    }
}
