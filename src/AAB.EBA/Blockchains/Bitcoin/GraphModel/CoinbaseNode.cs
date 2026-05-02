namespace AAB.EBA.Blockchains.Bitcoin.GraphModel;

public class CoinbaseNode : Node
{
    public CoinbaseNode() : base(id: Kind.ToString()) 
    { }

    public CoinbaseNode(
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null)
        : base(
            id: Kind.ToString(),
            originalInDegree: 0,
            originalOutDegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb)
    { }

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
