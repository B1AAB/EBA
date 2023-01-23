namespace BC2G.Graph.Db.Bulkload;

public enum Prop
{
    Height,
    ScriptAddress,
    ScriptType,
    BlockMedianTime,
    BlockConfirmations,
    BlockDifficulty,
    BlockTxCount,
    BlockSize,
    BlockStrippedSize,
    BlockWeight,
    NumGenerationEdges,
    NumTransferEdges,
    NumChangeEdges,
    NumFeeEdges,
    SumGenerationEdges,
    SumTransferEdges,
    SumChangeEdges,
    SumFeeEdges,
    EdgeSourceAddress,
    EdgeSourceType,
    EdgeTargetAddress,
    EdgeTargetType,
    EdgeType,
    EdgeValue
}

public enum FieldType
{
    String, Int, Float
}
