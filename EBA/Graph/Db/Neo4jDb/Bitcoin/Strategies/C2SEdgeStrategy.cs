using EBA.Graph.Bitcoin;
using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class C2SEdgeStrategy(bool serializeCompressed) : S2SEdgeStrategy(serializeCompressed)
{
    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            [
                $":START_ID({NodeLabels.Coinbase})",
                $":END_ID({ScriptNodeStrategy.Label})",
                Props.EdgeValue.TypeAnnotatedCsvHeader,
                Props.Height.TypeAnnotatedCsvHeader,
                ":TYPE"
            ]);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((C2SEdge)edge);
    }

    public static string GetCsv(C2SEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match header
        return string.Join(
            csvDelimiter,
            [
                NodeLabels.Coinbase.ToString(),
                edge.Target.Address,
                Helpers.Satoshi2BTC(edge.Value).ToString(),
                edge.BlockHeight.ToString(),
                edge.Type.ToString()
            ]);
    }

    public override string GetQuery(string csvFilename)
    {
        // The following is an example of the query this method generates.
        // Indentation and line breaks are added for the readiblity and 
        // are not included in the generated query.
        //
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv'
        // AS line FIELDTERMINATOR '	'
        //
        // MATCH (coinbase:Coinbase)
        // MATCH (target:Script {Address:line.TargetAddress})
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

        string l = Property.lineVarName, b = "block", s = "coinbase", t = "target";

        var builder = new StringBuilder(
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' ");

        builder.Append(
            $"MATCH ({s}:{NodeLabels.Coinbase}) " +
            $"MATCH ({t}:{ScriptNodeStrategy.Label} {{{Props.EdgeTargetAddress.GetSetter()}}}) " +
            $"MATCH ({b}:{BlockNodeStrategy.Label} {{{Props.Height.GetSetter()}}}) ");

        builder.Append(GetCreatesEdgeQuery(b, t) + " ");
        builder.Append($"WITH {l}, {b}, {s}, {t} ");

        builder.Append(GetApocCreateEdgeQuery(GetEdgePropertiesBase(), s, t));
        builder.Append(" RETURN distinct 'DONE'");

        return builder.ToString();
    }
}