namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class C2TEdgeStrategy : T2TEdgeStrategy
{
    /// Note that the ordre of the items in this array should 
    /// match those in the `ToCSV` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.T2TEdgeTargetTxid,
        Props.T2TEdgeTargetVersion,
        Props.T2TEdgeTargetSize,
        Props.T2TEdgeTargetVSize,
        Props.T2TEdgeTargetWeight,
        Props.T2TEdgeTargetLockTime,
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
        return GetCsv((C2TEdge)edge);
    }

    public static string GetCsv(C2TEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 
        return string.Join(csvDelimiter, new string[]
        {
            edge.Target.Txid,
            edge.Target.Version.ToString(),
            edge.Target.Size.ToString(),
            edge.Target.VSize.ToString(),
            edge.Target.Weight.ToString(),
            edge.Target.LockTime.ToString(),
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    public override string GetQuery(string csvFilename)
    {
        string l = Property.lineVarName, s = "coinbase", t = "target", b = "block";
        //var unknown = nameof(ScriptType.Unknown);

        return
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            $"MATCH ({s}:{BitcoinAgent.Coinbase}) " +

            GetNodeQuery(t, labels, Props.T2TEdgeTargetTxid, new List<Property>()
            {
                Props.T2TEdgeTargetVersion,
                Props.T2TEdgeTargetSize,
                Props.T2TEdgeTargetVSize,
                Props.T2TEdgeTargetWeight,
                Props.T2TEdgeTargetLockTime
            }) +
            " " +

            $"WITH {s}, {t}, {l} " +
            // Find the block
            GetBlockQuery(b) +
            " " +

            // Create edge between the script and its corresponding block
            CreatesEdgeQuery +

            $"WITH {s}, {t}, {l} " +
            // Create edge between the coinbase node and the script

            GetEdgeQuery(new List<Property>() { Props.EdgeValue, Props.Height }, s, t) +
            " " +
            $"RETURN distinct 'DONE'";
    }
}
