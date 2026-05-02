namespace AAB.EBA.Graph.Model;

public interface IValueReader
{
    TValue? GetValue<TValue>(string propName);
}

public readonly struct ValueReader(
    IReadOnlyDictionary<string, object> props) 
    : IValueReader
{
    public TValue? GetValue<TValue>(string propName)
    {
        if (props.TryGetValue(propName, out var val) && val != null)
        {
            // This is needed for types whose value are provided, but the type is nullable. 
            // when the value is provided but type is nullable, the following error is thrown:
            // Invalid cast from 'System.Int64' to 'System.Nullable`
            var targetType = typeof(TValue);
            var safeType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            return (TValue)Convert.ChangeType(val, safeType);
        }
        return default;
    }
}

public readonly struct ValueReader<TElement>(
    string[] cols, 
    ElementMapper<TElement> mapper) 
    : IValueReader
{
    public TValue? GetValue<TValue>(string propName)
    {
        return mapper.GetValue<TValue>(propName, cols);
    }
}
