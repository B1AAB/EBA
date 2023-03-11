namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class C2SEdgeStrategy : S2SEdgeStrategy
{
    /// Note that the ordre of the items in this array should 
    /// match those in the `ToCSV` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.EdgeTargetAddress,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    };

    public override string GetCsvHeader()
    {
        return string.Join(csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IEdge<Model.INode, Model.INode> edge)
    {
        return GetCsv((C2SEdge)edge);
    }

    public static string GetCsv(C2SEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(csvDelimiter, new string[]
        {
            edge.Target.Address,
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    public override string GetQuery(string csvFilename)
    {
        // The following is an example of the query this method generates.
        //
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv' AS line
        // FIELDTERMINATOR '	'
        //
        // MATCH (coinbase:Coinbase)
        // MATCH (block:Block {Height:toInteger(line.Height)})
        // MATCH (target:Script {Address:line.TargetAddress})
        //
        // MERGE (block)-[:Creates {Height:toInteger(line.Height)}]->(target)
        //
        // WITH line, block, coinbase, target
        // CALL apoc.merge.relationship(
        //   coinbase,
        //   line.EdgeType,
        //   {
        //     Value:toFloat(line.Value),
        //     Height:toInteger(line.Height)
        //   },
        //   {
        //     Count : 0
        //   },
        //   target,
        //   {})
        //
        // YIELD rel
        // SET rel.Count = rel.Count + 1
        // RETURN distinct 'DONE'
        //

        string l = Property.lineVarName, b = "block", s = "coinbase", t = "target";

        var builder = new StringBuilder(
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{csvDelimiter}' ");

        builder.Append(
            $"MATCH ({s}:{BitcoinAgent.Coinbase}) " +
            $"MATCH ({t}:{ScriptNodeStrategy.labels} {{{Props.EdgeTargetAddress.GetSetter()}}}) " +
            $"MATCH ({b}:{BlockGraphStrategy.labels} {{{Props.Height.GetSetter()}}}) ");

        builder.Append(GetCreatesEdgeQuery(b, t) + " ");
        builder.Append($"WITH {l}, {b}, {s}, {t} ");

        builder.Append(GetEdgeQuery(new List<Property>() { Props.EdgeValue, Props.Height }, s, t));
        builder.Append(" RETURN distinct 'DONE'");

        return builder.ToString();
    }
}
