using EBA.Graph.Bitcoin;
using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class C2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            [
                $":START_ID({NodeLabels.Coinbase})",
                $":END_ID({TxNodeStrategy.Label})",
                Props.EdgeValue,
                Props.Height,
                ":TYPE"
            ]);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((C2TEdge)edge);
    }

    public static string GetCsv(C2TEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match header.
        return string.Join(
            csvDelimiter,
            [
                NodeLabels.Coinbase.ToString(),
                edge.Target.Txid,
                Helpers.Satoshi2BTC(edge.Value).ToString(),
                edge.BlockHeight.ToString(),
                edge.Type.ToString()
            ]);
    }

    public override string GetQuery(string csvFilename)
    {
        // The following is an example of the query this method generates.
        // Indentation and linebreaks are added for the readability and 
        // not included in the gerated queries.
        //
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv' AS line
        // FIELDTERMINATOR '	'
        //
        // MATCH (coinbase:Coinbase)
        // MATCH (target:Tx {Txid:line.TargetId})
        // MATCH (block:Block {Height:toInteger(line.Height)})
        //
        // CREATE (block)-[:Creates {Height:toInteger(line.Height), Value:toFloat(line.Value)}]->(target)
        //
        // WITH line, block, coinbase, target
        //
        // CALL apoc.create.relationship(
        //     coinbase,
        //     line.EdgeType,
        //     {
        //         Height:toInteger(line.Height),
        //         Value:toFloat(line.Value)
        //     },
        //     target)
        // YIELD rel
        // RETURN distinct 'DONE'
        //

        string l = Property.lineVarName, s = "coinbase", t = "target", b = "block";

        var builder = new StringBuilder(
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' ");

        builder.Append(
            $"MATCH ({s}:{NodeLabels.Coinbase}) " +
            $"MATCH ({t}:{TxNodeStrategy.Label} {{{Props.T2TEdgeTargetTxid.GetSetter()}}}) " +
            $"MATCH ({b}:{BlockNodeStrategy.Label} {{{Props.Height.GetSetter()}}}) ");

        builder.Append(GetCreatesEdgeQuery(b, t) + " ");
        builder.Append($"WITH {l}, {b}, {s}, {t} ");

        builder.Append(GetApocCreateEdgeQuery(GetEdgePropertiesBase(), s, t));
        builder.Append(" RETURN distinct 'DONE'");

        return builder.ToString();
    }
}