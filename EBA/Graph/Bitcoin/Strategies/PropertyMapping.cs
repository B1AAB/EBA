using EBA.Graph.Db.Neo4jDb;
using System.Collections;

namespace EBA.Graph.Bitcoin.Strategies;

public class PropertyMapping<T>
{
    public Property Property { get; }
    private readonly Func<T, object?> _propertySelector;
    private readonly Func<Property, string>? _headerOverride;
    private readonly Func<object?, object?>? _deserializer;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "<Pending>")]
    public PropertyMapping(
        Property property,
        Func<T, object?> propertySelector,
        Func<Property, string>? headerOverride = null,
        Func<object?, object?>? deserializer = null)
    {
        Property = property;
        _propertySelector = propertySelector;
        _headerOverride = headerOverride;
        _deserializer = deserializer;
    }

    public PropertyMapping(
        string propertyLabel,
        FieldType neo4jNormalizedType,
        Func<T, object?> propertySelector,
        Func<Property, string>? headerOverride = null,
        Func<object?, object?>? deserializer = null) :
        this(
            new Property(propertyLabel, neo4jNormalizedType),
            propertySelector,
            headerOverride,
            deserializer)
    { }

    public string SerializeHeader()
    {
        return _headerOverride?.Invoke(Property) ?? Property.TypeAnnotatedCsvHeader;
    }

    public string SerializeValue(T source)
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

        if (_deserializer != null)
            return (V?)_deserializer(value);

        if (typeof(V).IsEnum)
            return (V)Enum.Parse(typeof(V), (string)value);

        if (value is V typed)
            return typed;

        if (value is IList list && typeof(V).IsArray)
        {
            // This is trying to avoid creating a collection that contains null items.

            var elementType = typeof(V).GetElementType()!;
            var buffer = new List<object>(list.Count);
            foreach (var item in list)
                if (item is not null)
                    buffer.Add(Convert.ChangeType(item, elementType));

            var array = Array.CreateInstance(elementType, buffer.Count);
            for (int i = 0; i < buffer.Count; i++)
                array.SetValue(buffer[i], i);

            return (V)(object)array;
        }

        return (V)Convert.ChangeType(value, typeof(V));
    }
}
