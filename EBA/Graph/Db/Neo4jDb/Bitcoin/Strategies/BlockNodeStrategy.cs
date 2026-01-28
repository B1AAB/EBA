using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class BlockNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Block;

    const Block b = null!;
    private static readonly PropertyMapping<BlockNode>[] _mappings =
    [
        new(nameof(b.Height), FieldType.Int, n => n.BlockMetadata.Height, p => p.GetIdFieldCsvHeader(Label.ToString())),
        new(nameof(b.Hash), FieldType.String, n => n.BlockMetadata.Hash),
        new(nameof(b.Confirmations), FieldType.Int, n => n.BlockMetadata.Confirmations),
        new(nameof(b.Version), FieldType.Int, n => n.BlockMetadata.Version),
        new(nameof(b.VersionHex), FieldType.String, n => n.BlockMetadata.VersionHex),
        new(nameof(b.Merkleroot), FieldType.String, n => n.BlockMetadata.Merkleroot),
        new(nameof(b.Time), FieldType.Int, n => n.BlockMetadata.Time),
        new(nameof(b.MedianTime), FieldType.Int, n => n.BlockMetadata.MedianTime),
        new(nameof(b.Nonce), FieldType.Int, n => n.BlockMetadata.Nonce),
        new(nameof(b.Bits), FieldType.String, n => n.BlockMetadata.Bits),
        new(nameof(b.Difficulty), FieldType.Float, n => n.BlockMetadata.Difficulty),
        new(nameof(b.Chainwork), FieldType.String, n => n.BlockMetadata.Chainwork),
        new(nameof(b.TransactionsCount), FieldType.Int, n => n.BlockMetadata.TransactionsCount),
        new(nameof(b.PreviousBlockHash), FieldType.String, n => n.BlockMetadata.PreviousBlockHash),
        new(nameof(b.NextBlockHash), FieldType.String, n => n.BlockMetadata.NextBlockHash),
        new(nameof(b.StrippedSize), FieldType.Int, n => n.BlockMetadata.StrippedSize),
        new(nameof(b.Size), FieldType.Int, n => n.BlockMetadata.Size),
        new(nameof(b.Weight), FieldType.Int, n => n.BlockMetadata.Weight),
        new(nameof(b.CoinbaseOutputsCount), FieldType.Int, n => n.BlockMetadata.CoinbaseOutputsCount),
        new(nameof(b.TxFees), FieldType.Int, n => n.BlockMetadata.TxFees),
        new(nameof(b.MintedBitcoins), FieldType.Int, n => n.BlockMetadata.MintedBitcoins),

        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(b.InputCounts), n => n.BlockMetadata.InputCounts),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(b.OutputCounts), n => n.BlockMetadata.OutputCounts),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(b.InputValues), n => n.BlockMetadata.InputValues),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(b.OutputValues), n => n.BlockMetadata.OutputValues),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(b.SpentOutputAge), n => n.BlockMetadata.SpentOutputAge),
        .. MappingHelpers.ScriptTypeCounts<BlockNode>(n => n.BlockMetadata.ScriptTypeCount),

        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];


    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((BlockNode)component);
    }

    public static string GetCsv(BlockNode node)
    {
        return _mappings.GetCsv(node);
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
        /*
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' " +
            $"MERGE ({block}:{Label} " +
            $"{{{Props.Height.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Height select x.GetSetter(block)));
        */
        return builder.ToString();
    }
}