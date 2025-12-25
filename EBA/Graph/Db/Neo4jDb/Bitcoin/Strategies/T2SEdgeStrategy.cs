using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class T2SEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            [
                $":START_ID({TxNodeStrategy.Label})",
                $":END_ID({ScriptNodeStrategy.Label})",
                Props.EdgeValue,
                Props.Height,
                ":TYPE"
            ]);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((T2SEdge)edge);
    }

    public static string GetCsv(T2SEdge edge)
    {
        return string.Join(
            csvDelimiter,
            [
                edge.Source.Txid,
                edge.Target.Address,
                Helpers.Satoshi2BTC(edge.Value).ToString(),
                edge.BlockHeight.ToString(),
                edge.Type.ToString(),
            ]);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}