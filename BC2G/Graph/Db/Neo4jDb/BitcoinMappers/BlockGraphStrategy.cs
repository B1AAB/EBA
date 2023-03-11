namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class BlockGraphStrategy : IGraphStrategy
{
    public const string labels = "Block";

    public const string csvDelimiter = "\t";
    public const string labelsDelimiter = ":";

    /// Note that the ordre of the items in this array should 
    /// match those returned from the `GetCsv()` method.
    private static readonly Property[] _properties = new Property[]
    {
        Props.Height,
        Props.BlockMedianTime,
        Props.BlockConfirmations,
        Props.BlockDifficulty,
        Props.BlockTxCount,
        Props.BlockSize,
        Props.BlockStrippedSize,
        Props.BlockWeight,
        Props.NumGenerationEdges,
        Props.NumTransferEdges,
        Props.NumChangeEdges,
        Props.NumFeeEdges,
        Props.SumGenerationEdges,
        Props.SumTransferEdges,
        Props.SumChangeEdges,
        Props.SumFeeEdges
    };

    public string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public string GetCsv(GraphBase g)
    {
        return GetCsv((BlockGraph)g);
    }

    public static string GetCsv(BlockGraph g)
    {
        var counts = g.Stats.EdgeTypeFrequency;
        var sums = g.Stats.EdgeTypeTxSum;

        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(csvDelimiter, new string[]
        {
        g.Block.Height.ToString(),
        g.Block.MedianTime.ToString(),
        g.Block.Confirmations.ToString(),
        g.Block.Difficulty.ToString(),
        g.Block.TransactionsCount.ToString(),
        g.Block.Size.ToString(),
        g.Block.StrippedSize.ToString(),
        g.Block.Weight.ToString(),
        counts[EdgeType.Generation].ToString(),
        counts[EdgeType.Transfer].ToString(),
        counts[EdgeType.Fee].ToString(),
        sums[EdgeType.Generation].ToString(),
        sums[EdgeType.Transfer].ToString(),
        sums[EdgeType.Fee].ToString()
        });
    }

    public virtual void ToCsv(GraphBase graph, string filename)
    {
        using var writer = new StreamWriter(filename, append: true);
        if (new FileInfo(filename).Length == 0)
            writer.WriteLine(GetCsvHeader());

        writer.WriteLine(GetCsv(graph));
    }

    public string GetQuery(string filename)
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
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            $"MERGE ({block}:{labels} " +
            $"{{{Props.Height.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Height select x.GetSetter(block)));

        return builder.ToString();
    }
}
