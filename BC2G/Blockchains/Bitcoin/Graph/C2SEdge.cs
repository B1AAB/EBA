namespace BC2G.Blockchains.Bitcoin.Graph;

public class C2SEdge : C2SEdge<ContextBase>
{
    public C2SEdge(
        ScriptNode<ContextBase> target,
        long value,
        uint timestamp,
        long blockHeight) : base(
            target,
            value,
            timestamp,
            blockHeight)
    { }
}

/// <summary>
/// Coinbase to Script edge.
/// This edge is implemented to simplify importing 
/// Coinbase->Script edges into Neo4j by implementing
/// Coinbase-specific logic and improvements.
/// </summary>
public class C2SEdge<T> : S2SEdge<T>
    where T : IContext
{
    public new static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinC2S; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinC2S;
    }

    public new EdgeLabel Label { get; } = EdgeLabel.C2SMinting;

    public C2SEdge(
        ScriptNode<T> target, long value, uint timestamp, long blockHeight) :
        base(
            ScriptNode<T>.GetCoinbaseNode(), target,
            value, EdgeType.Mints, timestamp, blockHeight)
    { }

    public new C2SEdge<T> Update(long value)
    {
        return new C2SEdge<T>(Target, Value + value, Timestamp, BlockHeight);
    }
}
