using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class BlockNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Block;

    /// Note that the ordre of the items in this array should 
    /// match those returned from the `GetCsv()` method.
    private static readonly Property[] _properties =
    [
        Props.BlockHash,
        Props.BlockConfirmations,
        Props.Height,
        Props.BlockVersion,
        Props.BlockVersionHex,
        Props.BlockMerkleroot,
        Props.BlockTime,
        Props.BlockMedianTime,
        Props.BlockNonce,
        Props.BlockBits,
        Props.BlockDifficulty,
        Props.BlockChainwork,
        Props.BlockTransactionsCount,
        Props.BlockPreviousBlockHash,
        Props.BlockNextBlockHash,
        Props.BlockStrippedSize,
        Props.BlockSize,
        Props.BlockWeight,
        Props.BlockCoinbaseOutputsCount,
        Props.BlockTxFees,
        Props.BlockMintedBitcoins
    ];

    public override string GetCsvHeader()
    {
        return string.Join(
            Options.CsvDelimiter,
            [
                Props.Height.GetIdFieldCsvHeader(Label.ToString()),
                Props.BlockHash.TypeAnnotatedCsvHeader,
                Props.BlockConfirmations.TypeAnnotatedCsvHeader,
                Props.BlockVersion.TypeAnnotatedCsvHeader,  
                Props.BlockVersionHex.TypeAnnotatedCsvHeader,
                Props.BlockMerkleroot.TypeAnnotatedCsvHeader,
                Props.BlockTime.TypeAnnotatedCsvHeader,
                Props.BlockMedianTime.TypeAnnotatedCsvHeader,
                Props.BlockNonce.TypeAnnotatedCsvHeader,
                Props.BlockBits.TypeAnnotatedCsvHeader,
                Props.BlockDifficulty.TypeAnnotatedCsvHeader,
                Props.BlockChainwork.TypeAnnotatedCsvHeader,
                Props.BlockTransactionsCount.TypeAnnotatedCsvHeader,
                Props.BlockPreviousBlockHash.TypeAnnotatedCsvHeader,
                Props.BlockNextBlockHash.TypeAnnotatedCsvHeader,
                Props.BlockStrippedSize.TypeAnnotatedCsvHeader,
                Props.BlockSize.TypeAnnotatedCsvHeader,
                Props.BlockWeight.TypeAnnotatedCsvHeader,
                Props.BlockCoinbaseOutputsCount.TypeAnnotatedCsvHeader,
                Props.BlockTxFees.TypeAnnotatedCsvHeader,
                Props.BlockMintedBitcoins.TypeAnnotatedCsvHeader,
                DescriptiveStatisticsStrategy.GetCsvHeader("InputsCounts"),
                DescriptiveStatisticsStrategy.GetCsvHeader("OutputCounts"),
                DescriptiveStatisticsStrategy.GetCsvHeader("InputValues"),
                DescriptiveStatisticsStrategy.GetCsvHeader("OutputValues"),
                DescriptiveStatisticsStrategy.GetCsvHeader("SpentOutputAge"),
                ":LABEL"
            ]);
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((BlockNode)component);
    }

    public static string GetCsv(BlockNode node)
    {
        var m = node.BlockMetadata;

        /// Note that the order of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(
            Options.CsvDelimiter,
            [
                m.Height.ToString(),
                m.Hash,
                m.Confirmations.ToString(),
                m.Version.ToString(),
                m.VersionHex,
                m.Merkleroot,
                m.Time.ToString(),
                m.MedianTime.ToString(),
                m.Nonce.ToString(),
                m.Bits,
                m.Difficulty.ToString(),
                m.Chainwork,
                m.TransactionsCount.ToString(),
                m.PreviousBlockHash,
                m.NextBlockHash,
                m.StrippedSize,
                m.Size,
                m.Weight,
                m.CoinbaseOutputsCount,
                m.TxFees,
                m.MintedBitcoins,
                DescriptiveStatisticsStrategy.GetCsv(m.InputCounts),
                DescriptiveStatisticsStrategy.GetCsv(m.OutputCounts),
                DescriptiveStatisticsStrategy.GetCsv(m.InputValues),
                DescriptiveStatisticsStrategy.GetCsv(m.OutputValues),
                DescriptiveStatisticsStrategy.GetCsv(m.SpentOutputAge),
                Label.ToString()
            ]);
    }

    public override string GetQuery(string filename)
    {
        // The following is an example of the generated query.
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv'
        // AS line FIELDTERMINATOR '	'
        // MERGE (block:Block {Height:toInteger(line.Height)})
        // SET
        //  block.MedianTime=line.MedianTime,
        //  block.Confirmations=toInteger(line.Confirmations),
        //  block.Difficulty=toFloat(line.Difficulty),
        //  block.TransactionsCount=toInteger(line.TransactionsCount),
        //  block.Size=toInteger(line.Size),
        //  block.StrippedSize=line.StrippedSize,
        //  block.Weight=toInteger(line.Weight),
        //  block.NumGenerationEdgeTypes=toInteger(line.NumGenerationEdgeTypes),
        //  block.NumTransferEdgeTypes=toInteger(line.NumTransferEdgeTypes),
        //  block.NumChangeEdgeTypes=toInteger(line.NumChangeEdgeTypes),
        //  block.NumFeeEdgeTypes=toInteger(line.NumFeeEdgeTypes),
        //  block.SumGenerationEdgeTypes=toFloat(line.SumGenerationEdgeTypes),
        //  block.SumTransferEdgeTypes=toFloat(line.SumTransferEdgeTypes),
        //  block.SumChangeEdgeTypes=toFloat(line.SumChangeEdgeTypes),
        //  block.SumFeeEdgeTypes=toFloat(line.SumFeeEdgeTypes)
        //

        string l = Property.lineVarName, block = "block";

        var builder = new StringBuilder();
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' " +
            $"MERGE ({block}:{Label} " +
            $"{{{Props.Height.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Height select x.GetSetter(block)));

        return builder.ToString();
    }
}