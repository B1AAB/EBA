namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

public class Property
{
    public const string lineVarName = "line";
    public string Name { get; }
    public string CsvHeader { get; }

    private readonly FieldType _type;

    public Property(string name, FieldType type = FieldType.String, string? csvHeader = null)
    {
        Name = name;
        CsvHeader = csvHeader ?? Name;
        _type = type;
    }

    public string GetLoadExp(string assignment = "=")
    {
        return _type switch
        {
            FieldType.Int => $"{Name}{assignment}toInteger({lineVarName}.{CsvHeader})",
            FieldType.Float => $"{Name}{assignment}toFloat({lineVarName}.{CsvHeader})",
            _ => $"{Name}{assignment}{lineVarName}.{CsvHeader}",
        };
    }

    public string GetLoad(string varName)
    {
        return $"{varName}.{Name} = COALESCE({lineVarName}.{CsvHeader}, {varName}.{Name})";
    }
}
