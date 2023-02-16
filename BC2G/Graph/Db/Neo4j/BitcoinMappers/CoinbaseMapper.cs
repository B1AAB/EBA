namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

internal class CoinbaseMapper : ScriptMapper
{
    /// Note that the ordre of the items in this array should 
    /// match those in the `ToCSV` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.EdgeTargetAddress,
        Props.EdgeTargetType,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    };

    public CoinbaseMapper(
        string workingDirectory,
        string cypherImportPrefix,
        string filename = "tmpBulkImportCoinbase.csv") :
        base(workingDirectory, cypherImportPrefix, /*importDirectory,*/ filename)
    { }

    public override string GetCsvHeader()
    {
        return string.Join(csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string ToCsv(S2SEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(csvDelimiter, new string[]
        {
            edge.Target.Address,
            edge.Target.ScriptType.ToString(),
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    protected override string ComposeCypherQuery(string filename)
    {
        var l = Property.lineVarName;

        return
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            $"MATCH (coinbase:{BitcoinAgent.Coinbase}) " +
            $"MERGE (target:{labels} {{" +
            $"{Props.EdgeTargetAddress.GetLoadExp(":")}}}) " +
            $"SET target.{Props.EdgeTargetType.GetLoadExp("=")} " +
            $"WITH coinbase, target, {l} " +
            $"MATCH (block:{BlockMapper.label} {{" +
            $"{Props.Height.GetLoadExp(":")}" +
            $"}}) " +
            // Create edge between the script and its corresponding block
            CreatesEdgeQuery +
            $"WITH coinbase, target, {l} " +
            // Create edge between the coinbase node and the script
            $"CALL apoc.merge.relationship(" +
            $"coinbase, " + // source
            $"{l}.{Props.EdgeType.CsvHeader}, " + // relationship type
            $"{{" + // properties
            $"{Props.EdgeValue.GetLoadExp(":")}, " +
            $"{Props.Height.GetLoadExp(":")}" +
            $"}}, " +
            $"{{ Count : 0 }}, " + // on create
            $"target, " + // target
            $"{{}}" + // on update
            $") " +
            $"YIELD rel " +
            $"SET rel.Count = rel.Count + 1 " +
            $"RETURN distinct 'DONE'";
    }
}
