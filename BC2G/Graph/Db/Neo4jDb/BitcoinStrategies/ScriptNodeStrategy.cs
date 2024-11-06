namespace BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

public class ScriptNodeStrategy : StrategyBase
{
    public const string labels = "Script";

    private readonly Property[] _properties = new Property[]
    {
        Props.ScriptAddress,
        Props.ScriptType
    };

    public override string GetCsvHeader()
    {
        return string.Join(
            Neo4jDb.csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((ScriptNode)component);
    }

    public static string GetCsv(ScriptNode node)
    {
        return string.Join(
            Neo4jDb.csvDelimiter,
            node.Address,
            node.ScriptType.ToString());
    }

    public override string GetQuery(string filename)
    {
        // The following is an example of the generated query.
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv' AS line
        // FIELDTERMINATOR '	'
        //
        // MERGE (node:Script {Address:line.Address})
        // ON CREATE
        //   SET
        //     node.ScriptType=line.ScriptType
        // ON MATCH
        //   SET
        //     node.ScriptType =
        //       CASE line.ScriptType
        //         WHEN 'Unknown'
        //         THEN node.ScriptType
        //         ELSE line.ScriptType
        //       END
        //

        string l = Property.lineVarName, node = "node";

        var builder = new StringBuilder();
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDb.csvDelimiter}' " +
            $"MERGE ({node}:{labels} {{{Props.ScriptAddress.GetSetter()}}}) ");

        builder.Append("ON CREATE SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.ScriptAddress select $"{x.GetSetter(node)}"));
        builder.Append(
            $" ON MATCH SET {node}.{Props.ScriptType.Name} = " +
            $"CASE {l}.{Props.ScriptType.CsvHeader} " +
            $"WHEN '{nameof(ScriptType.Unknown)}' THEN {node}.{Props.ScriptType.Name} " +
            $"ELSE {l}.{Props.ScriptType.CsvHeader} " +
            $"END");

        return builder.ToString();
    }
}
