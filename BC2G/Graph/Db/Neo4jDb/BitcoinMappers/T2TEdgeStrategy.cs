namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class T2TEdgeStrategy : BitcoinEdgeStrategy
{
    public const string labels = "Tx";

    /// Note that the ordre of the items in this array should 
    /// match those in the `GetCSV` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.T2TEdgeSourceTxid,
        Props.T2TEdgeSourceVersion,
        Props.T2TEdgeSourceSize,
        Props.T2TEdgeSourceVSize,
        Props.T2TEdgeSourceWeight,
        Props.T2TEdgeSourceLockTime,
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
        return string.Join(
            csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IEdge<Model.INode, Model.INode> edge)
    {
        return GetCsv((T2TEdge)edge);
    }

    public static string GetCsv(T2TEdge edge)
    {
        return string.Join(csvDelimiter, new string[]
        {
            edge.Source.Txid.ToString(),// != null ? edge.Source.Txid : double.NaN.ToString(),
            edge.Source.Version.ToString(),// != null ? edge.Source.Version.ToString(): double.NaN.ToString(),
            edge.Source.Size.ToString(), //!= null ? edge.Source.Size.ToString() : double.NaN.ToString(),
            edge.Source.VSize.ToString(), //!= null ? edge.Source.VSize.ToString() : double.NaN.ToString(),
            edge.Source.Weight.ToString(),// != null ? edge.Source.Weight.ToString() : double.NaN.ToString(),
            edge.Source.LockTime.ToString(),// != null ? edge.Source.LockTime.ToString() : double.NaN.ToString(),
            edge.Target.Txid,
            edge.Target.Version .ToString(),//!= null ? edge.Target.Version.ToString() : double.NaN.ToString(),
            edge.Target.Size.ToString(),// != null ? edge.Target.Size.ToString() : double.NaN.ToString(),
            edge.Target.VSize.ToString(),//  != null ? edge.Target.VSize.ToString() : double.NaN.ToString(),
            edge.Target.Weight.ToString(),// != null ? edge.Target.Weight.ToString() : double.NaN.ToString(),
            edge.Target.LockTime.ToString(),// != null ? edge.Target.LockTime.ToString() : double.NaN.ToString(),
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    public override string GetQuery(string csvFilename)
    {
        string l = Property.lineVarName, s = "source", t = "target", b = "block";

        return
           $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
           $"FIELDTERMINATOR '{csvDelimiter}' " +

           // Load source
           GetNodeQuery(s, labels, Props.T2TEdgeSourceTxid, new List<Property>()
           {
               Props.T2TEdgeSourceVersion,
               Props.T2TEdgeSourceSize,
               Props.T2TEdgeSourceVSize,
               Props.T2TEdgeSourceWeight,
               Props.T2TEdgeSourceLockTime
           }) +
           " " +

           // Load target
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

           // Create relationship between the block node and the Tx nodes. 
           RedeemsEdgeQuery +
           CreatesEdgeQuery +
           $"WITH {s}, {t}, {l} " +

           GetEdgeQuery(new List<Property>() { Props.EdgeValue, Props.Height }, s, t) +
           " " +
           // Create relationship between the source and target Tx nodes,
           // where the type of the relationship is read from the CSV file.

           $"RETURN distinct 'DONE'";
    }
}
