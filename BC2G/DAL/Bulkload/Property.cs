namespace BC2G.DAL.Bulkload;

internal class Property
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
}
