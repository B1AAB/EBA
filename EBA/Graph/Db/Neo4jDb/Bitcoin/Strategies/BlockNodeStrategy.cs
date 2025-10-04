using EBA.Graph.Db.Neo4jDb;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class BlockNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const string Labels = "Block";

    /// Note that the ordre of the items in this array should 
    /// match those returned from the `GetCsv()` method.
    private static readonly Property[] _properties =
    [
        Props.Height,
        Props.BlockMedianTime,
        Props.BlockConfirmations,
        Props.BlockDifficulty,
        Props.BlockTxCount,
        Props.BlockSize,
        Props.BlockStrippedSize,
        Props.BlockWeight
    ];

    public override string GetCsvHeader()
    {
        return string.Join(
            Neo4jDbLegacy.csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((BlockNode)component);
    }

    public static string GetCsv(BlockNode node)
    {
        /// Note that the order of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(
            Neo4jDbLegacy.csvDelimiter,
            [
                node.Height.ToString(),
                node.MedianTime.ToString(),
                node.Confirmations.ToString(),
                node.Difficulty.ToString(),
                node.TransactionsCount.ToString(),
                node.Size.ToString(),
                node.StrippedSize.ToString(),
                node.Weight.ToString(),
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
            $"MERGE ({block}:{Labels} " +
            $"{{{Props.Height.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Height select x.GetSetter(block)));

        return builder.ToString();
    }
}