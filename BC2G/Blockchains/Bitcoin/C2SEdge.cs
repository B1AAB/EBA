namespace BC2G.Blockchains.Bitcoin;

/// <summary>
/// Coinbase to Script edge.
/// This edge is implemented to simplify importing 
/// Coinbase->Script edges into Neo4j by implementing
/// Coinbase-specific logic and improvements.
/// </summary>
public class C2SEdge : S2SEdge
{
    public C2SEdge(
        ScriptNode target, double value, uint timestamp, long blockHeight) :
        base(
            ScriptNode.GetCoinbaseNode(), target,
            value, EdgeType.Generation, timestamp, blockHeight)
    { }

    public C2SEdge(
        ScriptNode source, ScriptNode target,
        IRelationship relationship) :
        base(source, target, relationship)
    { }
}
