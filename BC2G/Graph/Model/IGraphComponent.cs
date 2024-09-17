namespace BC2G.Graph.Model;

// Motivation behind implementing this interface.
//
// ComponentType is used to define a heterogeneous graph,
// which comprises diverse types of nodes and edges,
// each possessing distinct methods for serialization,
// importing into a graph database, and other functionalities.
//
// I have tried a few other alternatives that can be implemented
// without defining this type and enum. These alternatives are 
// briefly explained in the following. In short, the main drawback
// of the methods discussed in the following is that they rely on
// reflection that introduces considerable performance penatly
// when run at large scale, which motivated defining this interface.
//
// - System.Type is a good alternative. However, the draw back is
//   that when the System.Type is serialized to JSON, C# cannot 
//   get the System.Type at deserialization, which is required for 
//   getting type-specific strategies when loading serialized nodes
//   and edges into Neo4j (see Batch and StrategyFactory in Bitcoin Neo4j).
//   This functionality is not implemented in C# mainly for security
//   reasons; see the post in the following link for details:
//   https://github.com/dotnet/runtime/issues/30969#issuecomment-535779492
//
// - Serializing System.Type to string, and use the string value 
//   in the factory pattern is a also a good alternative. This was
//   implemented and tested. Functionaly-wise it works as expected,
//   however, since it requires Reflection to get the type given an
//   string, when run at scale, it introduced considerable
//   performance degredation.
//
// - Another method is using the `dynamic` keyword, get the type at runtime
//   and get some identifier. However, after some tests, it turns out 
//   the dynamic keyword can also introduce a significant performance penalty.
//

public enum GraphComponentType
{
    Undefined = 0,
    Graph = 1,
    Node = 2,
    Edge = 3,
    BitcoinGraph = 4,
    BitcoinTxGraph = 5,
    BitcoinC2T = 6,
    BitcoinC2S = 7,
    BitcoinT2T = 8,
    BitcoinS2S = 9,
    BitcoinTxNode = 10,
    BitcoinScriptNode = 11
}

public interface IGraphComponent
{
    public GraphComponentType ComponentType { get; }
}
