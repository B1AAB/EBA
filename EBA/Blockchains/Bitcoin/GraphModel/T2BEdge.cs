namespace EBA.Blockchains.Bitcoin.GraphModel;

public class T2BEdge(
    TxNode source,
    BlockNode target,
    long value,
    RelationType type,
    uint timestamp,
    long blockHeight) 
    : Edge<TxNode, BlockNode>(
        source,
        target,
        value,
        type,
        timestamp,
        blockHeight)
{ }
