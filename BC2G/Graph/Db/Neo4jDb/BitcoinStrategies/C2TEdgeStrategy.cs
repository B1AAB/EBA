namespace BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

public class C2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    /// Note that the ordre of the items in this array should 
    /// match those in the `ToCSV` method.
    private readonly Property[] _properties =
    [
        Props.T2TEdgeTargetTxid,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    ];

    public override string GetCsvHeader()
    {
        return string.Join(Neo4jDb.csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((C2TEdge)edge);
    }

    public static string GetCsv(C2TEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(Neo4jDb.csvDelimiter,
        [
            edge.Target.Txid,
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
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
            $"FIELDTERMINATOR '{Neo4jDb.csvDelimiter}' ");

        builder.Append(
            $"MATCH ({s}:{BitcoinAgent.Coinbase}) " +
            $"MATCH ({t}:{TxNodeStrategy.Labels} {{{Props.T2TEdgeTargetTxid.GetSetter()}}}) " +
            $"MATCH ({b}:{BlockNodeStrategy.Labels} {{{Props.Height.GetSetter()}}}) ");

        builder.Append(GetCreatesEdgeQuery(b, t) + " ");
        builder.Append($"WITH {l}, {b}, {s}, {t} ");

        builder.Append(GetApocCreateEdgeQuery(GetEdgePropertiesBase(), s, t));
        builder.Append(" RETURN distinct 'DONE'");

        return builder.ToString();
    }
}
