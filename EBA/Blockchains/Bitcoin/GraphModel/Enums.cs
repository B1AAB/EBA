namespace EBA.Blockchains.Bitcoin.GraphModel;

// Design Motivation
// 
//  1.  Type Identification
//      When you are reading a node or edge from the graph database,
//      you need a way to distinguish which type of node it is.
//      For instance, given a node instance fetched from the database,
//      you want to know whether it is a TxNode, BlockNode, ScriptNode, or CoinbaseNode.
//      Few options for this:
//
//      *   Runtime checks:
//          Using standard type checking
//          (e.g., if (node is TxNode)) is not possible because
//          the deserialized node from the database is a different,
//          generic type that stores properties in a dictionary
//          rather than as strongly-typed members.
//
//      *   Type serialization:
//          Serializing the specific node type string to
//          the database is possible, but it requires storing
//          implementation-specific info (which is not domain-informative),
//          and it will break with any future refactoring of the class name or namespace.
//
//  2.  Fixed Schema for Machine Learning
//      For some applications, you need to be aware of all
//      the different types of nodes or edges (triplets) in the graph schema.
//      For instance, if you want to report how many edges of different kinds
//      exist in the 2-hop neighborhood of a given node:
//
//      *   Without knowing all possible edge types,
//          you do not know which ones are missing, so you can only report on
//          the ones you see. This limits how you can serialize that info.
//          For example, you cannot use a tabular format (which is easier to
//          work with in ML settings); you are instead bound to formats like JSON,
//          where each property is an object and different objects may not
//          share the same set of keys.

//      *   For scenarios like this, having a fixed set of node and edge types
//          in the graph schema is useful. It allows you to define a fixed set
//          of features for each node and edge type.This makes it easier to
//          work with the graph data in an ML setting
//          (e.g., using a tabular format where each column is a feature and
//          each row is an instance of a node or edge).
//

public enum NodeKind
{
    Undefined = 0,
    Coinbase = 1,
    Script = 2,
    Block = 3,
    Tx = 4
}

public record EdgeKind(NodeKind Source, NodeKind Target, RelationType Relation)
{
    public override string ToString()
    {
        return $"{Source}-{Relation}-{Target}";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Target, Relation);
    }
}

public enum RelationType
{
    Mints = 0,
    Transfers = 1,
    Fee = 2,
    Redeems = 3,
    Confirms = 4,
    Credits = 5,

    /// <summary>
    /// The difference between this and Mints is that, 
    /// this includes both fee and minted coins.
    /// </summary>
    Rewards = 6
}