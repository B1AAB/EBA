using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class ScriptNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Script;

    private readonly static PropertyMapping<ScriptNode>[] _mappings =
    [
        new(nameof(ScriptNode.Address), FieldType.String, n => n.Address, p => p.GetIdFieldCsvHeader(Label.ToString())),
        new(nameof(ScriptNode.ScriptType), FieldType.String, n => n.ScriptType),
        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((ScriptNode)component);
    }

    public static string GetCsv(ScriptNode node)
    {
        return _mappings.GetCsv(node);
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
        /*
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' " +
            $"MERGE ({node}:{Label} {{{Props.ScriptAddress.GetSetter()}}}) ");

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
        */
        return builder.ToString();
    }
}