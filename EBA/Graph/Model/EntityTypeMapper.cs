using EBA.Graph.Bitcoin.Strategies;
using System.Linq.Expressions;

namespace EBA.Graph.Model;

public class EntityTypeMapper<T>
{
    private readonly PropertyMapping<T>[] _mappings;
    private readonly Dictionary<string, int> _propertyIndices;
    private readonly string _cachedCsvHeader;

    // The constructor computes expensive strings and lookups once!
    public EntityTypeMapper(PropertyMapping<T>[] mappings)
    {
        _mappings = mappings;

        _propertyIndices = new Dictionary<string, int>(_mappings.Length);
        for (int i = 0; i < _mappings.Length; i++)
            _propertyIndices[_mappings[i].Property.Name] = i;

        _cachedCsvHeader = string.Join(
            Options.CsvDelimiter,
            _mappings.Select(m => m.SerializeHeader()));
    }

    public string GetCsvHeader() => _cachedCsvHeader;

    public string ToCsvRow(T source)
    {
        return string.Join(Options.CsvDelimiter, _mappings.Select(m => m.SerializeValue(source)));
    }

    public PropertyMapping<T> Get(string propertyName)
    {
        if (_propertyIndices.TryGetValue(propertyName, out int index))
            return _mappings[index];

        throw new KeyNotFoundException($"No mapping found for property '{propertyName}'.");
    }

    public Dictionary<string, object?> ToProperties(T source)
    {
        var dict = new Dictionary<string, object?>(_mappings.Length);
        foreach (var m in _mappings)
            dict[m.Property.Name] = m.GetValue(source);
        return dict;
    }

    public int GetPropertyCsvIndex(string propertyName)
    {
        if (_propertyIndices.TryGetValue(propertyName, out int index))
            return index;

        throw new KeyNotFoundException($"No mapping found for property '{propertyName}'.");
    }

    public V? GetValue<V>(string propertyName, string[] csvRow)
    {
        if (!_propertyIndices.TryGetValue(propertyName, out int columnIndex))
            return default;

        // Safely return default if the CSV row is malformed or too short
        if (columnIndex >= csvRow.Length)
            return default;

        return _mappings[columnIndex].DeserializeCsv<V>(csvRow[columnIndex]);
    }

    public V GetValue<V>(
        string propertyName,
        IReadOnlyDictionary<string, object> properties)
    {
        if (!_propertyIndices.TryGetValue(propertyName, out int index))
            throw new KeyNotFoundException(
                $"No mapping found for property '{propertyName}'.");

        return 
            _mappings[index].Deserialize<V>(properties) 
            ?? throw new InvalidOperationException(
                $"The property '{propertyName}' returned null, " +
                $"but the requested type '{typeof(V).Name}' does not accept nulls.");
    }

    public V GetValue<V>(
        Expression<Func<T, V>> propertyExpression,
        IReadOnlyDictionary<string, object> properties)
    {
        var memberExpression = propertyExpression.Body switch
        {
            MemberExpression m => m,
            UnaryExpression { Operand: MemberExpression m } => m,
            _ => throw new ArgumentException("Expression must be a member access.")
        };

        return GetValue<V>(memberExpression.Member.Name, properties);
    }
}
