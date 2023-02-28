namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class BlockGraphStrategy : IGraphMapper
{
    public const string label = "Block";

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
        string comma = "";
        var propsBuilder = new StringBuilder();
        foreach (var p in _properties.Where(x => x.Name != Props.Height.Name))
        {
            propsBuilder.Append($"{comma}b.{p.GetLoadExp()}");
            comma = ", ";
        }

        var builder = new StringBuilder();
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {Property.lineVarName} " +
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            $"MERGE (b: {label} {{" +
            $"{Props.Height.GetLoadExp(":")}}})");

        // Using same props for both updating and creating seems redundant.
        // However, this covers a use-case where block was created without 
        // its properties (e.g., only block height was set), so it needs to 
        // be updated if exists. We could do something fancier to check 
        // for the missing fields and update them in case of `Match`, but 
        // that could make this Cypher query overly complicated. 
        //
        // A simplier update query can be a better replacement for this query.
        builder.Append(" ON CREATE SET ");
        builder.Append(propsBuilder);

        builder.Append(" ON MATCH SET ");
        builder.Append(propsBuilder);

        var x = builder.ToString();

        return builder.ToString();
    }
}
