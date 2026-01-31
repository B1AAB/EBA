using EBA.Graph.Bitcoin;
using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class C2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    public static readonly PropertyMapping<C2TEdge>[] _mappings =
    [
        MappingHelpers.SourceId<C2TEdge>(NodeLabels.Coinbase, _ => NodeLabels.Coinbase),
        MappingHelpers.TargetId<C2TEdge>(TxNodeStrategy.Label, e => e.Target.Txid),
        MappingHelpers.ValueBTCMapper<C2TEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        MappingHelpers.HeightMapper<C2TEdge>(e => e.BlockHeight),
        MappingHelpers.EdgeType<C2TEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((C2TEdge)edge);
    }

    public static string GetCsv(C2TEdge edge)
    {
        return _mappings.GetCsv(edge);
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
        /*
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
        
        return builder.ToString();*/
        return "";
    }
}