using EBA.Graph.Bitcoin;
using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public static class MappingHelpers
{
    public static string GetCsvHeader<T>(
        this PropertyMapping<T>[] mappings)
    {
        return string.Join(
            Options.CsvDelimiter,
            mappings.Select(m => m.GetHeader()));
    }

    public static string GetCsv<T>(
        this PropertyMapping<T>[] mappings,
        T source)
    {
        return string.Join(
            Options.CsvDelimiter,
            mappings.Select(m => m.GetValue(source)));
    }

    public static PropertyMapping<T> Height<T>(Func<T, long> getValue)
    {
        return new("Height", FieldType.Int, x => getValue(x));
    }
    public static PropertyMapping<T> Value<T>(Func<T, double> getValue)
    {
        return new("Value", FieldType.Float, x => getValue(x));
    }
    public static PropertyMapping<T> Address<T>(Func<T, string?> getValue)
    {
        return new("Address", FieldType.String, x => getValue(x));
    }

    public static PropertyMapping<T> SourceId<T>(NodeLabels label, Func<T, object?> getValue)
    {
        return new(":START_ID", FieldType.String, getValue, _ => $":START_ID({label})");
    }
    public static PropertyMapping<T> TargetId<T>(NodeLabels label, Func<T, object?> getValue)
    {
        return new(":END_ID", FieldType.String, getValue, _ => $":END_ID({label})");
    }
    public static PropertyMapping<T> EdgeType<T>(Func<T, object?> getType)
    {
        return new(":TYPE", FieldType.String, getType, _ => ":TYPE");
    }

    public static PropertyMapping<T> Label<T>(object label)
    {
        return new(":LABEL", FieldType.String, _ => label, _ => ":LABEL");
    }

    public static PropertyMapping<T>[] DescriptiveStats<T>(
        string prefix,
        Func<T, DescriptiveStatistics?> getStats)
    {
        // dummy variables to help with nameof expressions
        DescriptiveStatistics d = null!;
        DescriptiveStatistics.Percentile p = null!;

        return
        [
            new($"{prefix}.{nameof(d.Sum)}", FieldType.Float, s => getStats(s)?.Sum),
            new($"{prefix}.{nameof(d.Count)}", FieldType.Float, s => getStats(s)?.Count),
            new($"{prefix}.{nameof(d.Min)}", FieldType.Float, s => getStats(s)?.Min),
            new($"{prefix}.{nameof(d.Max)}", FieldType.Float, s => getStats(s)?.Max),
            new($"{prefix}.{nameof(d.Mean)}", FieldType.Float, s => getStats(s)?.Mean),
            new($"{prefix}.{nameof(d.Variance)}", FieldType.Float, s => getStats(s)?.Variance),
            new($"{prefix}.{nameof(d.Skewness)}", FieldType.Float, s => getStats(s)?.Skewness),
            new($"{prefix}.{nameof(d.Kurtosis)}", FieldType.Float, s => getStats(s)?.Kurtosis),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P01)}", FieldType.Float, s => getStats(s)?.Percentiles.P01),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P05)}", FieldType.Float, s => getStats(s)?.Percentiles.P05),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P25)}", FieldType.Float, s => getStats(s)?.Percentiles.P25),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P50)}", FieldType.Float, s => getStats(s)?.Percentiles.P50),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P75)}", FieldType.Float, s => getStats(s)?.Percentiles.P75),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P95)}", FieldType.Float, s => getStats(s)?.Percentiles.P95),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P99)}", FieldType.Float, s => getStats(s)?.Percentiles.P99),
        ];
    }

    public static PropertyMapping<T>[] ScriptTypeCounts<T>(
        Func<T, Dictionary<ScriptType, uint>> getScriptTypeCounts)
    {
        return [..
            Enum.GetValues<ScriptType>()
                .Select(scriptType => new PropertyMapping<T>(
                    $"ScriptType.{scriptType}",
                    FieldType.Int,
                    x => getScriptTypeCounts(x).GetValueOrDefault(scriptType)))];
    }

    public static DescriptiveStatistics ReadDescriptiveStats(
    IReadOnlyDictionary<string, object> properties,
    string prefix)
    {
        DescriptiveStatistics d = null!;
        DescriptiveStatistics.Percentile p = null!;

        return new DescriptiveStatistics
        {
            Sum = (double)properties[$"{prefix}.{nameof(d.Sum)}"],
            Count = (double)properties[$"{prefix}.{nameof(d.Count)}"],
            Min = (double)properties[$"{prefix}.{nameof(d.Min)}"],
            Max = (double)properties[$"{prefix}.{nameof(d.Max)}"],
            Mean = (double)properties[$"{prefix}.{nameof(d.Mean)}"],
            Variance = (double)properties[$"{prefix}.{nameof(d.Variance)}"],
            Skewness = (double)properties[$"{prefix}.{nameof(d.Skewness)}"],
            Kurtosis = (double)properties[$"{prefix}.{nameof(d.Kurtosis)}"],
            Percentiles = new DescriptiveStatistics.Percentile
            {
                P01 = (double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P01)}"],
                P05 = (double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P05)}"],
                P25 = (double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P25)}"],
                P50 = (double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P50)}"],
                P75 = (double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P75)}"],
                P95 = (double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P95)}"],
                P99 = (double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P99)}"],
            }
        };
    }

    public static Dictionary<ScriptType, uint> ReadScriptTypeCounts(
        IReadOnlyDictionary<string, object> properties)
    {
        return Enum.GetValues<ScriptType>()
            .ToDictionary(
                scriptType => scriptType,
                scriptType => (uint)(long)properties[$"ScriptType.{scriptType}"]);
    }
}