using EBA.Utilities;

using EBA.Utilities;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class B2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    /// Note that the ordre of the items in this array should 
    /// match those in the `GetCSV` method.
    private readonly Property[] _properties =
    [
        Props.Height,
        Props.Txid,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    ];

    public override string GetCsvHeader()
    {
        return string.Join(
            Neo4jDbLegacy.csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((B2TEdge)edge);
    }

    public static string GetCsv(B2TEdge edge)
    {
        return string.Join(Neo4jDbLegacy.csvDelimiter,
        [
            edge.Source.Height,
            edge.Target.Txid,
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