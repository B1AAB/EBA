using EBA.Graph.Db.Neo4jDb;

namespace EBA.Graph.Bitcoin.Strategies;

public class Property
{
    public const string lineVarName = "line";
    public const string createsEdgeLabel = "Creates";
    public const string redeemsEdgeLabel = "Redeems";

    public string Name { get; }
    public string CsvHeader { get; }
    public string TypeAnnotatedCsvHeader { get; }
    public FieldType Type { get; }

    public Property(string name, FieldType type = FieldType.String, string? csvHeader = null)
    {
        Name = name;
        CsvHeader = csvHeader ?? Name;
        Type = type;

        switch (type)
        {
            case FieldType.String:
            case FieldType.Int:
            case FieldType.Long:
            case FieldType.Float:
            case FieldType.Double:
                TypeAnnotatedCsvHeader = $"{Name}:{type.ToString().ToLower()}";
                break;

            case FieldType.StringArray:
                TypeAnnotatedCsvHeader = $"{Name}:{FieldType.String.ToString().ToLower()}[]";
                break;

            case FieldType.DoubleArray:
                TypeAnnotatedCsvHeader = $"{Name}:{FieldType.Double.ToString().ToLower()}[]";
                break;
        }
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
        return Type switch
        {
            FieldType.Int => $"toInteger({lineVarName}.{CsvHeader})",
            FieldType.Float => $"toFloat({lineVarName}.{CsvHeader})",
            _ => $"{lineVarName}.{CsvHeader}"
        };
    }
}