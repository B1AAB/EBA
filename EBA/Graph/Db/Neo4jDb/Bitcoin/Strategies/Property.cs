namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class Property
{
    public const string lineVarName = "line";
    public const string createsEdgeLabel = "Creates";
    public const string redeemsEdgeLabel = "Redeems";

    public string Name { get; }
    public string CsvHeader { get; }
    public string TypeAnnotatedCsvHeader { get; }

    private readonly FieldType _type;

    public Property(string name, FieldType type = FieldType.String, string? csvHeader = null)
    {
        Name = name;
        CsvHeader = csvHeader ?? Name;
        _type = type;
        TypeAnnotatedCsvHeader = $"{Name}:{type}";
    }

    public string GetIdFieldCsvHeader(string idGroup)
    {
        return $"{Name}:ID({idGroup})";
    }

    public string GetSetter()
    {
        return $"{Name}:{GetReader()}";
    }

    public string GetSetter(string varName, string assignment = "=")
    {
        return $"{varName}.{Name}{assignment}{GetReader()}";
    }

    public string GetSetterWithNullCheck(string varName)
    {
        return $"{varName}.{Name} = CASE {lineVarName}.{CsvHeader} WHEN \"\" THEN null ELSE {GetReader()} END";
    }

    public string GetReader()
    {
        return _type switch
        {
            FieldType.Int => $"toInteger({lineVarName}.{CsvHeader})",
            FieldType.Float => $"toFloat({lineVarName}.{CsvHeader})",
            _ => $"{lineVarName}.{CsvHeader}"
        };
    }
}