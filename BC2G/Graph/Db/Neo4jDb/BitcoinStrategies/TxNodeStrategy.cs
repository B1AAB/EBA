namespace BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

public class TxNodeStrategy : StrategyBase
{
    public const string labels = "Tx";

    private readonly Property[] _properties =
    [
        Props.Txid,
        Props.TxVersion,
        Props.TxSize,
        Props.TxVSize,
        Props.TxWeight,
        Props.TxLockTime,
        Props.HasIncomingEdges
    ];

    public override string GetCsvHeader()
    {
        return string.Join(
            Neo4jDb.csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((TxNode)component);
    }

    public static string GetCsv(TxNode node)
    {
        return string.Join(
            Neo4jDb.csvDelimiter,
            node.Txid,
            node.Version,
            node.Size,
            node.VSize,
            node.Weight,
            node.LockTime,
            node.IncomingEdges.Count > 0);
    }

    public override string GetQuery(string filename)
    {
        // The following is an example of the generated query.
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv'
        // AS line FIELDTERMINATOR '	'
        // MERGE (node:Tx {Txid:line.Txid})
        // SET
        //  node.Version = CASE line.SourceVersion WHEN "" THEN null ELSE toInteger(line.SourceVersion) END,
        //  node.Size = CASE line.SourceSize WHEN "" THEN null ELSE toInteger(line.SourceSize) END,
        //  node.VSize = CASE line.SourceVSize WHEN "" THEN null ELSE toInteger(line.SourceVSize) END,
        //  node.Weight = CASE line.SourceWeight WHEN "" THEN null ELSE toInteger(line.SourceWeight) END,
        //  node.LockTime = CASE line.SourceLockTime WHEN "" THEN null ELSE toInteger(line.SourceLockTime) END 
        //

        string l = Property.lineVarName, node = "node";

        var builder = new StringBuilder();
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDb.csvDelimiter}' " +
            $"MERGE ({node}:{labels} {{{Props.Txid.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Txid select $"{x.GetSetterWithNullCheck(node)}"));

        return builder.ToString();
    }
}
