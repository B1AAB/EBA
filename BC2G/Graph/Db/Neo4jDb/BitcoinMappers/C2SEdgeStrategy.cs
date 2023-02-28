namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class C2SEdgeStrategy : S2SEdgeStrategy
{
    /// Note that the ordre of the items in this array should 
    /// match those in the `ToCSV` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.EdgeTargetAddress,
        Props.EdgeTargetType,
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
            edge.Target.ScriptType.ToString(),
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    public override string GetQuery(string csvFilename)
    {
        string l = Property.lineVarName, s = "coinbase", t = "target", b="block";

        return
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            $"MATCH ({s}:{BitcoinAgent.Coinbase}) " +

            GetNodeQuery(t, labels, Props.EdgeTargetAddress, Props.EdgeTargetType) +
            " " +
            $"WITH {s}, {t}, {l} " +
            GetBlockQuery(b) +
            " "+

            // Create edge between the script and its corresponding block
            CreatesEdgeQuery +
            " " +
            $"WITH {s}, {t}, {l} " +
            // Create edge between the coinbase node and the script

            GetEdgeQuery(new List<Property>() { Props.EdgeValue, Props.Height }, s, t) +
            " " +
            $"RETURN distinct 'DONE'";
    }
}
