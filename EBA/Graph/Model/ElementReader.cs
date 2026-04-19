namespace EBA.Graph.Model;

public interface IElementReader // TODO: not sure if this name is accurate
{
    TValue? GetValue<TValue>(string propName);
}

public readonly struct ElementReader(
    IReadOnlyDictionary<string, object> props) 
    : IElementReader
{
    public TValue? GetValue<TValue>(string propName)
    {
        if (props.TryGetValue(propName, out var val) && val != null)
            return (TValue)Convert.ChangeType(val, typeof(TValue));
        return default;
    }
}

public readonly struct ElementReader<TElement>(
    string[] cols, 
    ElementMapper<TElement> mapper) 
    : IElementReader
{
    public TValue? GetValue<TValue>(string propName)
    {
        return mapper.GetValue<TValue>(propName, cols);
    }
}
