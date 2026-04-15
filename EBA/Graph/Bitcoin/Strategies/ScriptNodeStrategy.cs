namespace EBA.Graph.Bitcoin.Strategies;

public class ScriptNodeStrategy(bool serializeCompressed) 
    : StrategyBase<ScriptNode, ScriptNodeStrategy>(
        $"nodes_{ScriptNode.Kind}",
        serializeCompressed),
    IElementSchema<ScriptNode>
{
    public static string IdSpace { get; } = ScriptNode.Kind.ToString();

    public static EntityTypeMapper<ScriptNode> Mapper { get; } = new EntityTypeMapper<ScriptNode>(
        new MappingBuilder<ScriptNode>()
            .Map(n => n.SHA256Hash).WithCsvHeader(p => p.GetIdFieldCsvHeader(IdSpace))
            .Map(n => n.Address)
            .Map(n => n.ScriptType)
            .Map(n => n.HexBase64)
            .MapLabel(_ => ScriptNode.Kind)
            .ToArray());

    public static ScriptNode Deserialize(
        Neo4j.Driver.INode node,
        double? originalIndegree,
        double? originalOutdegree,
        double? hopsFromRoot)
    {
        return new ScriptNode(
            address: Mapper.GetValue(x => x.Address, node.Properties),
            scriptType: Mapper.GetValue(x => x.ScriptType, node.Properties),
            sha256Hash: Mapper.GetValue(x => x.SHA256Hash, node.Properties)!,
            hexBase64: Mapper.GetValue(x => x.HexBase64, node.Properties),
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