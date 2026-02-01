namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

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
}
