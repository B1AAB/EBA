namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class PropertyMapping<T>(
    string name,
    FieldType type,
    Func<T, object?> getValue,
    Func<Property, string>? headerOverride = null)
{
    public Property Property { get; } = new Property(name, type);
    private readonly Func<T, object?> _getValue = getValue;
    private readonly Func<Property, string>? _headerOverride = headerOverride;

    public string GetHeader()
    {
        return _headerOverride?.Invoke(Property) ?? Property.TypeAnnotatedCsvHeader;
    }

    public string GetValue(T source)
    {
        return _getValue(source)?.ToString() ?? string.Empty;
    }

    public TValue ReadFrom<TValue>(IReadOnlyDictionary<string, object> properties)
    {
        var value = properties[Property.Name];
        return Property.Type switch
        {
            FieldType.Int => (TValue)(object)Convert.ToInt64(value),
            FieldType.Float => (TValue)(object)Convert.ToDouble(value),
            _ => (TValue)value
        };
    }
}
