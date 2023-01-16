namespace BC2G.DAL.Bulkload;

internal class BlockMapper : ModelMapper<BlockGraph>
{
    public const string label = "Block";

    /// Note that the ordre of the items in this array should 
    /// match those returned from the `ToCsv()` method.. 
    private static readonly Prop[] _properties = new Prop[]
    {
        Prop.Height,
        Prop.BlockMedianTime,
        Prop.BlockConfirmations,
        Prop.BlockDifficulty,
        Prop.BlockTxCount,
        Prop.BlockSize,
        Prop.BlockStrippedSize,
        Prop.BlockWeight,
        Prop.NumGenerationEdges,
        Prop.NumTransferEdges,
        Prop.NumChangeEdges,
        Prop.NumFeeEdges,
        Prop.SumGenerationEdges,
        Prop.SumTransferEdges,
        Prop.SumChangeEdges,
        Prop.SumFeeEdges
    };

    public BlockMapper(
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
            from x in _properties select Props[x].CsvHeader);
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
            $"{Props[Prop.Height].GetLoadExp(":")}}})" +
            $"ON CREATE SET ");

        string comma = "";
        foreach (var p in _properties) if (p != Prop.Height)
            {
                builder.Append($"{comma}b.{Props[p].GetLoadExp()}");
                comma = ", ";
            }

        builder.Append(
            " ON MATCH SET " +
            $"b.{Props[Prop.BlockConfirmations].GetLoadExp()}");

        return builder.ToString();
    }
}
