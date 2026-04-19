namespace EBA.Graph.Model;

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
            return (TValue)Convert.ChangeType(val, typeof(TValue));
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
