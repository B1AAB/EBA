namespace EBA.Graph.Bitcoin.Strategies;

public static class PropertyMappingExtensions
{
    public static string GetCsvHeader<T>(
    this PropertyMapping<T>[] mappings)
    {
        return string.Join(
            Options.CsvDelimiter,
            mappings.Select(m => m.SerializeHeader()));
    }

    public static string GetCsv<T>(
        this PropertyMapping<T>[] mappings,
        T source)
    {
        return string.Join(
            Options.CsvDelimiter,
            mappings.Select(m => m.SerializeValue(source)));
    }

    public static PropertyMapping<T> Get<T>(this PropertyMapping<T>[] mappings, string propertyName)
    {
        foreach (var m in mappings)
            if (m.Property.Name == propertyName)
                return m;

        throw new KeyNotFoundException($"No mapping found for property '{propertyName}'.");
    }
}
