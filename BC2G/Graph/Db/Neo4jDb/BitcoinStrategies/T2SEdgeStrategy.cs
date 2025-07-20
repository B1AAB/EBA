using BC2G.Utilities;

namespace BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

public class T2SEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    /// Note that the ordre of the items in this array should 
    /// match those in the `GetCSV` method.
    private readonly Property[] _properties =
    [
        Props.T2SEdgeSourceTxid,
        Props.T2SEdgeTargetTxid,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    ];

    public override string GetCsvHeader()
    {
        return string.Join(
            Neo4jDb.csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((T2SEdge)edge);
    }

    public static string GetCsv(T2SEdge edge)
    {
        return string.Join(Neo4jDb.csvDelimiter,
        [
            edge.Source.Txid,
            edge.Target.Address,
            edge.Type.ToString(),
            Helpers.Satoshi2BTC(edge.Value).ToString(),
            edge.BlockHeight.ToString()
        ]);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
