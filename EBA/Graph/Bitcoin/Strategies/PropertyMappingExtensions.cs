namespace EBA.Graph.Bitcoin.Strategies;

public static class PropertyMappingExtensions
{/*
    public static string GetCsvHeader<T>(
    this PropertyMapping<T>[] mappings)
    {
        return string.Join(
            Options.CsvDelimiter,
            mappings.Select(m => m.SerializeHeader()));
    }*/
    /*
    public static string GetCsv<T>(
        this PropertyMapping<T>[] mappings,
        T source)
    {
        return string.Join(
            Options.CsvDelimiter,
            mappings.Select(m => m.SerializeValue(source)));
    }*/
    /*
    public static PropertyMapping<T> Get<T>(this PropertyMapping<T>[] mappings, string propertyName)
    {
        foreach (var m in mappings)
            if (m.Property.Name == propertyName)
                return m;

        throw new KeyNotFoundException($"No mapping found for property '{propertyName}'.");
    }*/

    // TODO: should not need the following
    public static Dictionary<string, object?> ToDictionary<T>(
        this PropertyMapping<T>[] mappings,
        T source)
    {
        var dict = new Dictionary<string, object?>(mappings.Length);
        foreach (var m in mappings)
            dict[m.Property.Name] = m.GetValue(source);
        return dict;
    }

    // Maps a property name directly to the corresponding index in the parsed CSV string array
    /*public static V? GetCsvValue<T, V>(this PropertyMapping<T>[] mappings, string propertyName, string[] csvRow)
    {
        int columnIndex = Array.FindIndex(mappings, m => m.Property.Name == propertyName);

        // If the mapping isn't found, or the CSV row is malformed/too short, return default safely
        if (columnIndex < 0 || columnIndex >= csvRow.Length)
            return default;

        return mappings[columnIndex].DeserializeCsv<V>(csvRow[columnIndex]);
    }*/
}
