using EBA.Utilities;

namespace EBA.Graph.Model;

public static class PropertyMappingFactory
{
    public static string GetLabel(string prefix, EdgeKind edgeKind)
    {
        // Make sure to keep EdgeKind string representation compatible with neo4j header requirements. 
        return $"{prefix}.{edgeKind.Source}_{edgeKind.Relation}_{edgeKind.Target}";
    }

    public static string GetLabel<T>() where T : struct, Enum
    {
        return typeof(T).Name;
    }

    public static string GetLabel(string prefix, string enumTypeName, string enumValueName)
    {
        return $"{prefix}.{enumTypeName}.{enumValueName}";
    }

    public static PropertyMapping<T>[] ToMappings<T, TEnum, TValue>(
        string prefix,
        Func<T, Dictionary<TEnum, TValue>?> getDict)
        where TEnum : struct, Enum
    {
        var builder = new MappingBuilder<T>();
        var enumTypeName = GetLabel<TEnum>();

        foreach (var v in Enum.GetValues<TEnum>())
        {
            builder.Map(
                $"{GetLabel(prefix, enumTypeName, v.ToString())}",
                x =>
                {
                    var dict = getDict(x);
                    return dict != null && dict.TryGetValue(v, out var val) ? val : default;
                });
        }

        return builder.ToArray();
    }

    public static Dictionary<TEnum, TValue> GetDictionary<TEnum, TValue>(
        string prefix,
        IReadOnlyDictionary<string, object> properties)
        where TEnum : struct, Enum
    {
        var enumTypeName = GetLabel<TEnum>();
        var dict = new Dictionary<TEnum, TValue>();

        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            var key = GetLabel(prefix, enumTypeName, enumValue.ToString());
            if (properties.TryGetValue(key, out var rawValue) && rawValue != null)
                dict[enumValue] = (TValue)Convert.ChangeType(rawValue, typeof(TValue));
        }

        return dict;
    }

    public static PropertyMapping<T>[] ToMappings<T>(
        string prefix,
        Func<T, DescriptiveStatistics?> getStats,
        Func<double?, double>? converter = null)
    {
        // dummy variables to help with nameof expressions
        DescriptiveStatistics d = null!;
        DescriptiveStatistics.Percentile p = null!;

        var C = converter ?? (x => x ?? double.NaN);

        return new MappingBuilder<T>()
            .Map($"{prefix}.{nameof(d.Sum)}", s => C(getStats(s)?.Sum))
            .Map($"{prefix}.{nameof(d.Count)}", s => C(getStats(s)?.Count))
            .Map($"{prefix}.{nameof(d.Min)}", s => C(getStats(s)?.Min))
            .Map($"{prefix}.{nameof(d.Max)}", s => C(getStats(s)?.Max))
            .Map($"{prefix}.{nameof(d.Mean)}", s => C(getStats(s)?.Mean))
            .Map($"{prefix}.{nameof(d.Variance)}", s => C(getStats(s)?.Variance))
            .Map($"{prefix}.{nameof(d.Skewness)}", s => C(getStats(s)?.Skewness))
            .Map($"{prefix}.{nameof(d.Kurtosis)}", s => C(getStats(s)?.Kurtosis))
            .Map($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P01)}", s => C(getStats(s)?.Percentiles.P01))
            .Map($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P05)}", s => C(getStats(s)?.Percentiles.P05))
            .Map($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P25)}", s => C(getStats(s)?.Percentiles.P25))
            .Map($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P50)}", s => C(getStats(s)?.Percentiles.P50))
            .Map($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P75)}", s => C(getStats(s)?.Percentiles.P75))
            .Map($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P95)}", s => C(getStats(s)?.Percentiles.P95))
            .Map($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P99)}", s => C(getStats(s)?.Percentiles.P99))
            .ToArray();
    }

    public static DescriptiveStatistics ReadDescriptiveStats(
        IReadOnlyDictionary<string, object> properties,
        string prefix,
        Func<double, double>? converter = null)
    {
        DescriptiveStatistics d = null!;
        DescriptiveStatistics.Percentile p = null!;

        var C = converter ?? (x => x);

        return new DescriptiveStatistics
        {
            Sum = C((double)properties[$"{prefix}.{nameof(d.Sum)}"]),
            Count = C((double)properties[$"{prefix}.{nameof(d.Count)}"]),
            Min = C((double)properties[$"{prefix}.{nameof(d.Min)}"]),
            Max = C((double)properties[$"{prefix}.{nameof(d.Max)}"]),
            Mean = C((double)properties[$"{prefix}.{nameof(d.Mean)}"]),
            Variance = C((double)properties[$"{prefix}.{nameof(d.Variance)}"]),
            Skewness = C((double)properties[$"{prefix}.{nameof(d.Skewness)}"]),
            Kurtosis = C((double)properties[$"{prefix}.{nameof(d.Kurtosis)}"]),
            Percentiles = new DescriptiveStatistics.Percentile
            {
                P01 = C((double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P01)}"]),
                P05 = C((double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P05)}"]),
                P25 = C((double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P25)}"]),
                P50 = C((double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P50)}"]),
                P75 = C((double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P75)}"]),
                P95 = C((double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P95)}"]),
                P99 = C((double)properties[$"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P99)}"]),
            }
        };
    }

    public static PropertyMapping<T>[] ToMappings<T>(
        Func<T, OHLCV?> getOhlcv,
        string prefix = "OHLCV")
    {
        OHLCV o = null!;
        return new MappingBuilder<T>()
            .Map($"{prefix}.{nameof(o.Open)}", s => (double?)(getOhlcv(s)?.Open))
            .Map($"{prefix}.{nameof(o.High)}", s => (double?)(getOhlcv(s)?.High))
            .Map($"{prefix}.{nameof(o.Low)}", s => (double?)(getOhlcv(s)?.Low))
            .Map($"{prefix}.{nameof(o.Close)}", s => (double?)(getOhlcv(s)?.Close))
            .Map($"{prefix}.{nameof(o.Volume)}", s => getOhlcv(s)?.Volume)
            .Map($"{prefix}.{nameof(o.VWAP)}", s => (double?)(getOhlcv(s)?.VWAP))
            .Map($"{prefix}.{nameof(o.OHLC4)}", s => (double?)(getOhlcv(s)?.OHLC4))
            .ToArray();
    }

    public static OHLCV? ReadOHLCV(
        IReadOnlyDictionary<string, object> properties,
        string prefix = "OHLCV")
    {
        OHLCV o;
        if (!properties.TryGetValue($"{prefix}.{nameof(o.Open)}", out _))
            return null;

        return new OHLCV(
            timestamp: 0,
            open: (decimal)(double)properties[$"{prefix}.{nameof(o.Open)}"],
            high: (decimal)(double)properties[$"{prefix}.{nameof(o.High)}"],
            low: (decimal)(double)properties[$"{prefix}.{nameof(o.Low)}"],
            close: (decimal)(double)properties[$"{prefix}.{nameof(o.Close)}"],
            volume: (long)properties[$"{prefix}.{nameof(o.Volume)}"],
            vwap: (decimal)(double)properties[$"{prefix}.{nameof(o.VWAP)}"]);
    }


    public static PropertyMapping<T>[] ToMappings<T, TValue>(
        string prefix,
        Func<T, Dictionary<EdgeKind, TValue>?> getDict)
    {
        var builder = new MappingBuilder<T>();

        foreach (var kind in Schema.EdgeKinds)
        {
            builder.Map(GetLabel(prefix, kind), x =>
            {
                var dict = getDict(x);
                return dict != null && dict.TryGetValue(kind, out var v) ? v : default;
            });
        }

        return builder.ToArray();
    }

    public static Dictionary<EdgeKind, TValue> GetDictionary<TValue>(
        string prefix,
        IReadOnlyDictionary<string, object> properties)
    {
        var dict = new Dictionary<EdgeKind, TValue>();

        foreach (var kind in Schema.EdgeKinds)
        {
            if (properties.TryGetValue(GetLabel(prefix, kind), out var rawValue) && rawValue != null)
            {
                dict[kind] = (TValue)Convert.ChangeType(rawValue, typeof(TValue));
            }
        }

        return dict;
    }
}