using EBA.Graph.Db.Neo4jDb;
using System.Collections;

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
        var value = _propertySelector(source);
        if (value is null)
            return string.Empty;

        if (value is not string && value is IEnumerable enumerable)
        {
            var items = new List<string>();
            foreach (var item in enumerable)
                items.Add(item?.ToString() ?? string.Empty);
            
            return string.Join(';', items);
        }

        return value.ToString() ?? string.Empty;
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

        if (value is IList list && typeof(V).IsArray)
        {
            var elementType = typeof(V).GetElementType()!;
            var array = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var converted = item != null
                    ? Convert.ChangeType(item, elementType)
                    : null;
                array.SetValue(converted, i);
            }
            return (V)(object)array;
        }

        return (V)Convert.ChangeType(value, typeof(V));
    }
}
