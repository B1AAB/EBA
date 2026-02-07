namespace EBA.Blockchains.Bitcoin.GraphModel;

public class NonStandardScriptNode : Node, IComparable<NonStandardScriptNode>, IEquatable<NonStandardScriptNode>
{
    public static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinNonStandardScriptNode; }
    }
    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinNonStandardScriptNode;
    }

    public string Hex { get; }
    public string HexBase64{ get; }

    public NonStandardScriptNode(Output output) : base(id: output.ScriptPubKey.SHA256HashString)
    {
        Hex = output.ScriptPubKey.Hex;
        HexBase64 = output.ScriptPubKey.Base64String;
    }

    public int CompareTo(NonStandardScriptNode? other)
    {
        throw new NotImplementedException();
    }

    public bool Equals(NonStandardScriptNode? other)
    {
        throw new NotImplementedException();
    }
}
