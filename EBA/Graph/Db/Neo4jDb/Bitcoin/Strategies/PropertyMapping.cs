namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;


public class PropertyMapping<TEntity>(
    string propertyLabel,
    FieldType neo4jNormalizedType,
    Func<TEntity, object?> getValue,
    Func<Property, string>? headerOverride = null)
{
    public Property Property { get; } = new Property(propertyLabel, neo4jNormalizedType);
    private readonly Func<TEntity, object?> _getValue = getValue;
    private readonly Func<Property, string>? _headerOverride = headerOverride;

    public string GetHeader()
    {
        return _headerOverride?.Invoke(Property) ?? Property.TypeAnnotatedCsvHeader;
    }

    public string GetValue(TEntity source)
    {
        return _getValue(source)?.ToString() ?? string.Empty;
    }

    public V? ReadFrom<V>(IReadOnlyDictionary<string, object> properties)
    {
        object? value = properties.GetValueOrDefault(Property.Name);
        if (value == null)
            return default;

        if (typeof(V).IsEnum)
            return (V)Enum.Parse(typeof(V), (string)value);

        if (value is V typed)
            return typed;

        return (V)Convert.ChangeType(value, typeof(V));
    }
}
