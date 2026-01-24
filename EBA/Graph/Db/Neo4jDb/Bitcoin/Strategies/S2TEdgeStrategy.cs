using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class S2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            [
                $":START_ID({ScriptNodeStrategy.Label})",
                $":END_ID({TxNodeStrategy.Label})",
                Props.EdgeValue.TypeAnnotatedCsvHeader,
                Props.Height.TypeAnnotatedCsvHeader,
                Props.CreatedInBlockHeight.TypeAnnotatedCsvHeader,
                ":TYPE"
            ]);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((S2TEdge)edge);
    }

    public static string GetCsv(S2TEdge edge)
    {
        return string.Join(
            csvDelimiter,
            [
                edge.Source.Address,
                edge.Target.Txid,
                Helpers.Satoshi2BTC(edge.Value).ToString(),
                edge.BlockHeight.ToString(),
                edge.UTxOCreatedInBockHeight.ToString(),
                edge.Type.ToString(),
            ]);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}