namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

public class T2TEdgeMapper : BitcoinMapperBase
{
    public const string labels = "Tx";

    /// Note that the ordre of the items in this array should 
    /// match those in the `GetCSV` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.T2TEdgeSourceTxid,
        Props.T2TEdgeTargetTxid,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    };

    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IEdge<Model.INode, Model.INode> edge)
    {
        return GetCsv((T2TEdge)edge);
    }

    public static string GetCsv(T2TEdge edge)
    {
        return string.Join(csvDelimiter, new string[]
        {
            edge.Source.Tx.Txid,
            edge.Target.Tx.Txid,
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    public override string GetQuery(string csvFilename)
    {
        throw new NotImplementedException();
    }
}
