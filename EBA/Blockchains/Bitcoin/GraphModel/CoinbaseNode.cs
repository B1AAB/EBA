namespace EBA.Blockchains.Bitcoin.GraphModel;

public class CoinbaseNode : Node
{
    public CoinbaseNode(
        Neo4j.Driver.INode node,
        double? originalOutdegree = null,
        double? hopsFromRoot = null) : 
        base(id: _kind.ToString(),
            kind: _kind,
            originalInDegree: 0,
            originalOutDegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: node.ElementId)
    { }

    private static readonly NodeKind _kind = NodeKind.Coinbase;

    public override string GetIdPropertyName()
    {
        return _kind.ToString();
    }

    public static new string[] GetFeaturesName()
    {
        return [_kind.ToString()];
    }

    public override string[] GetFeatures()
    {
        return ["0"];
    }
}
