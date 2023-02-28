namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

internal class BlockStrategy : ModelStrategy<BlockGraph>
{
    public const string label = "Block";

    /// Note that the ordre of the items in this array should 
    /// match those returned from the `ToCsv()` method.
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

    public BlockStrategy(
        string workingDirectory,
        string importPrefix,
        //string importDirectory,
        string filename = "tmpBulkImportBlocks.csv") :
        base(workingDirectory, importPrefix, /*importDirectory,*/ filename)
    { }

    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string ToCsv(BlockGraph bgraph)
    {
        var counts = bgraph.Stats.EdgeTypeFrequency;
        var sums = bgraph.Stats.EdgeTypeTxSum;

        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(csvDelimiter, new string[]
        {
            bgraph.Block.Height.ToString(),
            bgraph.Block.MedianTime.ToString(),
            bgraph.Block.Confirmations.ToString(),
            bgraph.Block.Difficulty.ToString(),
            bgraph.Block.TransactionsCount.ToString(),
            bgraph.Block.Size.ToString(),
            bgraph.Block.StrippedSize.ToString(),
            bgraph.Block.Weight.ToString(),
            counts[EdgeType.Generation].ToString(),
            counts[EdgeType.Transfer].ToString(),
            counts[EdgeType.Fee].ToString(),
            sums[EdgeType.Generation].ToString(),
            sums[EdgeType.Transfer].ToString(),
            sums[EdgeType.Fee].ToString()
        });
    }

    protected override string ComposeCypherQuery(string filename)
    {
        var builder = new StringBuilder();
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {Property.lineVarName} " +
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            $"MERGE (b: {label} {{" +
            $"{Props.Height.GetLoadExp(":")}}})" +
            $"ON CREATE SET ");

        string comma = "";
        foreach (var p in _properties)
        {
            if (p.Name != Props.Height.Name)
            {
                builder.Append($"{comma}b.{p.GetLoadExp()}");
                comma = ", ";
            }
        }

        builder.Append(
            " ON MATCH SET " +
            $"b.{Props.BlockConfirmations.GetLoadExp()}");

        return builder.ToString();
    }
}
