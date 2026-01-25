using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

namespace EBA.Graph.Model;

// TODO: this could possibly be better implemented as an attribute on the properties themselves.

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
    public static Property CreatedInBlockHeight { get; } = new("CreatedInBlockHeight", FieldType.Int);
    public static Property ScriptAddress { get; } = new(_addressProperty);
    public static Property ScriptType { get; } = new(_scriptTypeProperty);
    public static Property Txid { get; } = new(_txNodeTxid);
    public static Property TxVersion { get; } = new Property(_txNodeVersion, FieldType.Int, "SourceVersion");
    public static Property TxSize { get; } = new Property(_txNodeSize, FieldType.Int, "SourceSize");
    public static Property TxVSize { get; } = new Property(_txNodeVSize, FieldType.Int, "SourceVSize");
    public static Property TxWeight { get; } = new Property(_txNodeWeight, FieldType.Int, "SourceWeight");
    public static Property TxLockTime { get; } = new Property(_txNodeLockTime, FieldType.Int, "SourceLockTime");
    public static Property BlockTxCount { get; } = new("TransactionsCount", FieldType.Int);
    public static Property EdgeSourceAddress { get; } = new(_addressProperty, csvHeader: "SourceAddress");
    public static Property EdgeTargetAddress { get; } = new(_addressProperty, csvHeader: "TargetAddress");
    public static Property EdgeType { get; } = new("EdgeType");
    public static Property EdgeValue { get; } = new("Value", FieldType.Float);
    public static Property T2TEdgeSourceTxid { get; } = new Property(_txNodeTxid, csvHeader: "SourceId");
    public static Property T2TEdgeTargetTxid { get; } = new Property(_txNodeTxid, csvHeader: "TargetId");
    public static Property S2TEdgeSourceTxid { get; } = new Property(_addressProperty, csvHeader: "SourceId");
    public static Property S2TEdgeTargetTxid { get; } = new Property(_txNodeTxid, csvHeader: "TargetId");
    public static Property T2SEdgeSourceTxid { get; } = new Property(_txNodeTxid, csvHeader: "SourceId");
    public static Property T2SEdgeTargetTxid { get; } = new Property(_addressProperty, csvHeader: "TargetId");



    public static Property BlockHash { get; } = new("Hash", FieldType.String);
    public static Property BlockConfirmations { get; } = new("Confirmations", FieldType.Int);
    public static Property BlockVersion { get; } = new("Version", FieldType.Int);
    public static Property BlockVersionHex { get; } = new("VersionHex", FieldType.String);
    public static Property BlockMerkleroot { get; } = new("Merkleroot", FieldType.String);
    public static Property BlockTime { get; } = new("Time", FieldType.Int);
    public static Property BlockMedianTime { get; } = new("MedianTime", FieldType.Int);
    public static Property BlockNonce { get; } = new("Nonce", FieldType.Int);
    public static Property BlockBits { get; } = new("Bits", FieldType.String);
    public static Property BlockDifficulty { get; } = new("Difficulty", FieldType.Float);
    public static Property BlockChainwork { get; } = new("Chainwork", FieldType.String);
    public static Property BlockTransactionsCount { get; } = new("TransactionsCount", FieldType.Int);
    public static Property BlockPreviousBlockHash { get; } = new("PreviousBlockHash", FieldType.String);
    public static Property BlockNextBlockHash { get; } = new("NextBlockHash", FieldType.String);
    public static Property BlockStrippedSize { get; } = new("StrippedSize", FieldType.Int);
    public static Property BlockSize { get; } = new("Size", FieldType.Int);
    public static Property BlockWeight { get; } = new("Weight", FieldType.Int);
    public static Property BlockCoinbaseOutputsCount { get; } = new("CoinbaseOutputsCount", FieldType.Int);
    public static Property BlockTxFees { get; } = new("TxFees", FieldType.Int);
    public static Property BlockMintedBitcoins { get; } = new("MintedBitcoins", FieldType.Int);

    public static string BlockInputCountsPrefix = "InputCounts";
    public static DescriptiveStatisticsProperties BlockInputCounts = new(BlockInputCountsPrefix);

    public static string BlockOutputCountsPrefix = "OutputCounts";
    public static DescriptiveStatisticsProperties BlockOutputCounts = new(BlockOutputCountsPrefix);

    public static string BlockInputValuesPrefix = "InputValues";
    public static DescriptiveStatisticsProperties BlockInputValues = new(BlockInputValuesPrefix);

    public static string BlockOutputValuesPrefix = "OutputValues";
    public static DescriptiveStatisticsProperties BlockOutputValues = new(BlockOutputValuesPrefix);

    public static string BlockSpentOutputAgePrefix = "SpentOutputAge";
    public static DescriptiveStatisticsProperties BlockSpentOutputAge = new(BlockSpentOutputAgePrefix);

    public class DescriptiveStatisticsProperties(string prefix)
    {
        public Property Sum { get; } = new($"{prefix}.Sum", FieldType.Float);
        public Property Count { get; } = new($"{prefix}.Count", FieldType.Float);
        public Property Min { get; } = new($"{prefix}.Min", FieldType.Float);
        public Property Max { get; } = new($"{prefix}.Max", FieldType.Float);
        public Property Mean { get; } = new($"{prefix}.Mean", FieldType.Float);
        public Property Variance { get; } = new($"{prefix}.Variance", FieldType.Float);
        public Property Skewness { get; } = new($"{prefix}.Skewness", FieldType.Float);
        public Property Kurtosis { get; } = new($"{prefix}.Kurtosis", FieldType.Float);
        public Property Percentile_P01 { get; } = new($"{prefix}.Percentiles.P01", FieldType.Float);
        public Property Percentile_P05 { get; } = new($"{prefix}.Percentiles.P05", FieldType.Float);
        public Property Percentile_P25 { get; } = new($"{prefix}.Percentiles.P25", FieldType.Float);
        public Property Percentile_P50 { get; } = new($"{prefix}.Percentiles.P50", FieldType.Float);
        public Property Percentile_P75 { get; } = new($"{prefix}.Percentiles.P75", FieldType.Float);
        public Property Percentile_P95 { get; } = new($"{prefix}.Percentiles.P95", FieldType.Float);
        public Property Percentile_P99 { get; } = new($"{prefix}.Percentiles.P99", FieldType.Float);
    }
}
