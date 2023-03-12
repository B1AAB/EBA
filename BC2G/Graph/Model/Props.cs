using BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

namespace BC2G.Graph.Model;

public static class Props
{
    private const string _txNodeTxid = "Txid";
    private const string _txNodeVersion = "Version";
    private const string _txNodeSize = "Size";
    private const string _txNodeVSize = "VSize";
    private const string _txNodeWeight = "Weight";
    private const string _txNodeLockTime = "LockTime";

    private const string _addressProperty = "Address";
    private const string _scriptTypeProperty = "ScriptType";

    public static Property Height { get; } = new("Height", FieldType.Int);
    public static Property ScriptAddress { get; } = new(_addressProperty);
    public static Property ScriptType { get; } = new(_scriptTypeProperty);
    public static Property Txid { get; } = new(_txNodeTxid);
    public static Property TxVersion { get; } = new Property(_txNodeVersion, FieldType.Int, "SourceVersion");
    public static Property TxSize { get; } = new Property(_txNodeSize, FieldType.Int, "SourceSize");
    public static Property TxVSize { get; } = new Property(_txNodeVSize, FieldType.Int, "SourceVSize");
    public static Property TxWeight { get; } = new Property(_txNodeWeight, FieldType.Int, "SourceWeight");
    public static Property TxLockTime { get; } = new Property(_txNodeLockTime, FieldType.Int, "SourceLockTime");
    public static Property BlockMedianTime { get; } = new("MedianTime");
    public static Property BlockConfirmations { get; } = new("Confirmations", FieldType.Int);
    public static Property BlockDifficulty { get; } = new("Difficulty", FieldType.Float);
    public static Property BlockTxCount { get; } = new("TransactionsCount", FieldType.Int);
    public static Property BlockSize { get; } = new("Size", FieldType.Int);
    public static Property BlockStrippedSize { get; } = new("StrippedSize");
    public static Property BlockWeight { get; } = new("Weight", FieldType.Int);
    public static Property NumGenerationEdges { get; } = new("NumGenerationEdgeTypes", FieldType.Int);
    public static Property NumTransferEdges { get; } = new("NumTransferEdgeTypes", FieldType.Int);
    public static Property NumChangeEdges { get; } = new("NumChangeEdgeTypes", FieldType.Int);
    public static Property NumFeeEdges { get; } = new("NumFeeEdgeTypes", FieldType.Int);
    public static Property SumGenerationEdges { get; } = new("SumGenerationEdgeTypes", FieldType.Float);
    public static Property SumTransferEdges { get; } = new("SumTransferEdgeTypes", FieldType.Float);
    public static Property SumChangeEdges { get; } = new("SumChangeEdgeTypes", FieldType.Float);
    public static Property SumFeeEdges { get; } = new("SumFeeEdgeTypes", FieldType.Float);
    public static Property EdgeSourceAddress { get; } = new(_addressProperty, csvHeader: "SourceAddress");
    public static Property EdgeTargetAddress { get; } = new(_addressProperty, csvHeader: "TargetAddress");
    public static Property EdgeType { get; } = new("EdgeType");
    public static Property EdgeValue { get; } = new("Value", FieldType.Float);
    public static Property T2TEdgeSourceTxid { get; } = new Property(_txNodeTxid, csvHeader: "SourceId");
    public static Property T2TEdgeTargetTxid { get; } = new Property(_txNodeTxid, csvHeader: "TargetId");
}
