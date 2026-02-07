namespace EBA.Blockchains.Bitcoin.GraphModel;

public class NullDataNode : Node, IComparable<NullDataNode>, IEquatable<NullDataNode>
{
    public static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinNullDataNode; }
    }
    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinNullDataNode;
    }

    public string Hex { get; }
    public string HexBase64 { get; }

    public NullDataNode(Output output) : base(id: output.ScriptPubKey.SHA256HashString)
    {
        Hex = output.ScriptPubKey.Hex;
        HexBase64 = output.ScriptPubKey.Base64String;
    }

    public NullDataNode(
        string id,
        double? originalIndegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        base(
            id: id,
            originalInDegree: originalIndegree,
            originalOutDegree: 0,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb)
    { }


    public int CompareTo(NullDataNode? other)
    {
        throw new NotImplementedException();
    }

    public bool Equals(NullDataNode? other)
    {
        throw new NotImplementedException();
    }
}
