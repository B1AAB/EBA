using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class B2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            [
                $":START_ID({BlockNodeStrategy.Label})",
                $":END_ID({TxNodeStrategy.Label})",
                Props.EdgeValue.TypeAnnotatedCsvHeader,
                Props.Height.TypeAnnotatedCsvHeader,
                ":TYPE"
            ]);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((B2TEdge)edge);
    }

    public static string GetCsv(B2TEdge edge)
    {
        return string.Join(
            csvDelimiter,
            [
                edge.Source.BlockMetadata.Height,
                edge.Target.Txid,
                Helpers.Satoshi2BTC(edge.Value).ToString(),
                edge.BlockHeight.ToString(),
                edge.Type.ToString()
            ]);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}