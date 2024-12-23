namespace BC2G.Blockchains.Bitcoin.Graph;

/// <summary>
/// Coinbase to Script edge.
/// This edge is implemented to simplify importing 
/// Coinbase->Script edges into Neo4j by implementing
/// Coinbase-specific logic and improvements.
/// </summary>
public class C2SEdge : S2SEdge
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinC2S; }
    }

    public new EdgeLabel Label { get; } = EdgeLabel.C2SMinting;

    public C2SEdge(
        ScriptNode target, double value, uint timestamp, long blockHeight) :
        base(
            ScriptNode.GetCoinbaseNode(), target,
            value, EdgeType.Mints, timestamp, blockHeight)
    { }

    public new C2SEdge Update(double value)
    {
        return new C2SEdge(Target, Value + value, Timestamp, BlockHeight);
    }
}
