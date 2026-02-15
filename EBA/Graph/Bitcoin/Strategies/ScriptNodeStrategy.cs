using EBA.Graph.Db.Neo4jDb;

namespace EBA.Graph.Bitcoin.Strategies;

public class ScriptNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Script;

    private const ScriptNode v = null!;
    private static readonly PropertyMapping<ScriptNode> _address = 
        PropertyMappingFactory.Address<ScriptNode>(
            n => n.Address, 
            p => p.GetIdFieldCsvHeader(Label.ToString()));

    private readonly static PropertyMapping<ScriptNode>[] _mappings =
    [
        _address,
        new(nameof(v.ScriptType), FieldType.String, n => n.ScriptType),
        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];

    private static readonly Dictionary<string, PropertyMapping<ScriptNode>> _mappingsDict =
        _mappings.ToDictionary(m => m.Property.Name, m => m);

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement component)
    {
        return GetCsv((ScriptNode)component);
    }

    public static string GetCsv(ScriptNode node)
    {
        return _mappings.GetCsv(node);
    }

    public static ScriptNode Deserialize(
        Neo4j.Driver.INode node,
        double? originalIndegree,
        double? originalOutdegree,
        double? hopsFromRoot)
    {
        return new ScriptNode(
            address: _address.Deserialize<string>(node.Properties),
            scriptType: _mappingsDict[nameof(v.ScriptType)].Deserialize<ScriptType>(node.Properties),
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            hopsFromRoot: hopsFromRoot,
            idInGraphDb: node.ElementId.ToString());
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
        /*
        string l = Property.lineVarName, node = "node";

        var builder = new StringBuilder();
        
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
        
        return builder.ToString();
        */
        throw new NotImplementedException();
    }
}