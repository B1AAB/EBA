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

    public object? GetValue(T source) => _propertySelector(source);

    public string SerializeHeader()
    {
        return _headerOverride?.Invoke(Property) ?? Property.CsvHeaderTypeAnnotated;
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

    private V? ConvertValue<V>(object? rawValue)
    {
        if (rawValue == null || (rawValue is string str && string.IsNullOrEmpty(str)))
            return default;

        if (_deserializer != null)
            return (V?)_deserializer(rawValue);

        if (typeof(V).IsEnum)
        {
            return (V)Enum.Parse(typeof(V), rawValue.ToString()!);
        }

        if (rawValue is V typedValue)
            return typedValue;

        if (typeof(V).IsArray)
        {
            var elementType = typeof(V).GetElementType()!;
            
            if (rawValue is string csvString)
            {
                var parts = csvString.Split(';');
                var array = Array.CreateInstance(elementType, parts.Length);
                for (int i = 0; i < parts.Length; i++)
                    if (!string.IsNullOrEmpty(parts[i]))
                        array.SetValue(Convert.ChangeType(parts[i], elementType), i);
                        
                return (V)(object)array;
            }
            else if (rawValue is IList list)
            {
                int count = 0;
                for (int i = 0; i < list.Count; i++)
                    if (list[i] is not null) 
                        count++;

                var array = Array.CreateInstance(elementType, count);
                int idx = 0;
                for (int i = 0; i < list.Count; i++)
                    if (list[i] is not null)
                        array.SetValue(Convert.ChangeType(list[i], elementType), idx++);

                return (V)(object)array;
            }
        }

        Type underlyingType = Nullable.GetUnderlyingType(typeof(V)) ?? typeof(V);
        return (V)Convert.ChangeType(rawValue, underlyingType);
    }

    public V? Deserialize<V>(IReadOnlyDictionary<string, object> properties)
    {
        return properties.TryGetValue(Property.Name, out var value) 
            ? ConvertValue<V>(value) 
            : default;
    }

    public V? DeserializeCsv<V>(string stringValue)
    {
        return ConvertValue<V>(stringValue);
    }
}
