namespace EBA.Graph.Bitcoin.Strategies;


public class PropertyMapping<TEntity>
{
    public Property Property { get; }
    private readonly Func<TEntity, object?> _propertySelector;
    private readonly Func<Property, string>? _headerOverride;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "<Pending>")]
    public PropertyMapping(
    Property property,
    Func<TEntity, object?> propertySelector,
    Func<Property, string>? headerOverride = null)
    {
        Property = property;
        _propertySelector = propertySelector;
        _headerOverride = headerOverride;
    }

    public PropertyMapping(
        string propertyLabel,
        FieldType neo4jNormalizedType,
        Func<TEntity, object?> propertySelector,
        Func<Property, string>? headerOverride = null) :
        this(
            new Property(propertyLabel, neo4jNormalizedType), 
            propertySelector, 
            headerOverride)
    { }

    public string SerializeHeader()
    {
        return _headerOverride?.Invoke(Property) ?? Property.TypeAnnotatedCsvHeader;
    }

    public string SerializeValue(TEntity source)
    {
        return _propertySelector(source)?.ToString() ?? string.Empty;
    }

    public V? Deserialize<V>(IReadOnlyDictionary<string, object> properties)
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
