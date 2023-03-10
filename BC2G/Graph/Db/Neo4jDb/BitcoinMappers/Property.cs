namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

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

    public string GetLoadExpOld(string assignment = "=")
    {
        return _type switch
        {
            FieldType.Int => $"{Name}{assignment}toInteger({lineVarName}.{CsvHeader})",
            FieldType.Float => $"{Name}{assignment}toFloat({lineVarName}.{CsvHeader})",
            _ => $"{Name}{assignment}{lineVarName}.{CsvHeader}",
        };
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

    private string GetReader()
    {
        return _type switch
        {
            FieldType.Int => $"toInteger({lineVarName}.{CsvHeader})",
            FieldType.Float => $"toFloat({lineVarName}.{CsvHeader})",
            _ => $"{lineVarName}.{CsvHeader}"
        };
    }
}
