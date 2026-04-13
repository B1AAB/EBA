namespace EBA.Graph.Bitcoin.Strategies;

public class ScriptNodeStrategy(bool serializeCompressed) 
    : BitcoinStrategyBase(
        $"nodes_{ScriptNode.Kind}",
        serializeCompressed)
{
    public static string IdSpace { get; } = ScriptNode.Kind.ToString();

    public readonly static PropertyMapping<ScriptNode>[] Mappings = new MappingBuilder<ScriptNode>()
        .Map(n => n.SHA256Hash).WithCsvHeader(p => p.GetIdFieldCsvHeader(IdSpace))

        .Map(n => n.Address)
        .Map(n => n.ScriptType)
        .Map(n => n.HexBase64)
        .MapLabel(_ => ScriptNode.Kind)

        .ToArray();

    private static readonly Dictionary<string, PropertyMapping<ScriptNode>> _mappingsDict =
        Mappings.ToDictionary(m => m.Property.Name, m => m);

    public override string GetCsvHeader()
    {
        return Mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement component)
    {
        return GetCsv((ScriptNode)component);
    }

    public static string GetCsv(ScriptNode node)
    {
        return Mappings.GetCsv(node);
    }

    public static ScriptNode Deserialize(
        Neo4j.Driver.INode node,
        double? originalIndegree,
        double? originalOutdegree,
        double? hopsFromRoot)
    {
        return new ScriptNode(
            address: _mappingsDict[nameof(ScriptNode.Address)].Deserialize<string>(node.Properties),
            scriptType: _mappingsDict[nameof(ScriptNode.ScriptType)].Deserialize<ScriptType>(node.Properties),
            sha256Hash: _mappingsDict[nameof(ScriptNode.SHA256Hash)].Deserialize<string>(node.Properties)!,
            hexBase64: _mappingsDict[nameof(ScriptNode.HexBase64)].Deserialize<string>(node.Properties),
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

    public override string[] GetSchemaConfigs()
    {
        return 
        [
            $"CREATE CONSTRAINT {ScriptNode.Kind}_{nameof(ScriptNode.SHA256Hash)}_Unique " +
            $"IF NOT EXISTS " +
            $"FOR (v:{ScriptNode.Kind}) REQUIRE v.{nameof(ScriptNode.SHA256Hash)} IS UNIQUE"
        ];
    }
}