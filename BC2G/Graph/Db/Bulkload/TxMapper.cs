namespace BC2G.Graph.Db.Bulkload;

internal class TxMapper : ModelMapper<T2TEdge>
{
    public const string labels = "Tx";

    /// Note that the ordre of the items in this array should 
    /// match those in the `ToCSV` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.EdgeSourceAddress,
        Props.EdgeSourceType,
        Props.EdgeTargetAddress,
        Props.EdgeTargetType,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    };

    public TxMapper(
        string workingDirectory,
        string cypherImportPrefix,
        string filename = "tmpTxImportEdges.csv") :
        base(workingDirectory, cypherImportPrefix, filename)
    { }

    public override string GetCsvHeader()
    {
        return string.Join(csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string ToCsv(T2TEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(csvDelimiter, new string[]
        {
            edge.Source.Tx.Txid,
            edge.Target.Tx.Txid,
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    protected override string ComposeCypherQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
