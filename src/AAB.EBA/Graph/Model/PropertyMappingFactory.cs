using AAB.EBA.Utilities;

namespace AAB.EBA.Graph.Model;

public static class PropertyMappingFactory
{
    public static char PropertyNameDelimiter => '.';

    private static string GetLabel(string prefix, EdgeKind edgeKind)
    {
        // Make sure to keep EdgeKind string representation compatible with neo4j header requirements. 
        return $"{prefix}.{edgeKind.Source}_{edgeKind.Relation}_{edgeKind.Target}";
    }

    private static string GetLabel<T>() where T : struct, Enum
    {
        return typeof(T).Name;
    }

    private static string GetLabel(string prefix, string enumTypeName, string enumValueName)
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

    public static PropertyMapping<T>[] ToMappings<T>(
        string prefix,
        Func<T, DescriptiveStatistics?> getStats,
        Func<double?, double>? converter = null)
    {
        // dummy variables to help in string interpolation
        DescriptiveStatistics d = null!;
        DescriptiveStatistics.Percentile p = null!;
        var o = PropertyNameDelimiter;

        var C = converter ?? (x => x ?? double.NaN);

        return new MappingBuilder<T>()
            .Map($"{prefix}{o}{nameof(d.Sum)}", s => C(getStats(s)?.Sum))
            .Map($"{prefix}{o}{nameof(d.Count)}", s => C(getStats(s)?.Count))
            .Map($"{prefix}{o}{nameof(d.Min)}", s => C(getStats(s)?.Min))
            .Map($"{prefix}{o}{nameof(d.Max)}", s => C(getStats(s)?.Max))
            .Map($"{prefix}{o}{nameof(d.Mean)}", s => C(getStats(s)?.Mean))
            .Map($"{prefix}{o}{nameof(d.Variance)}", s => C(getStats(s)?.Variance))
            .Map($"{prefix}{o}{nameof(d.Skewness)}", s => C(getStats(s)?.Skewness))
            .Map($"{prefix}{o}{nameof(d.Kurtosis)}", s => C(getStats(s)?.Kurtosis))
            .Map($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P01)}", s => C(getStats(s)?.Percentiles.P01))
            .Map($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P05)}", s => C(getStats(s)?.Percentiles.P05))
            .Map($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P25)}", s => C(getStats(s)?.Percentiles.P25))
            .Map($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P50)}", s => C(getStats(s)?.Percentiles.P50))
            .Map($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P75)}", s => C(getStats(s)?.Percentiles.P75))
            .Map($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P95)}", s => C(getStats(s)?.Percentiles.P95))
            .Map($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P99)}", s => C(getStats(s)?.Percentiles.P99))
            .ToArray();
    }

    public static DescriptiveStatistics ReadDescriptiveStats<TReader>(
        TReader reader,
        string prefix,
        Func<double, double>? converter = null)
        where TReader : IValueReader
    {
        DescriptiveStatistics d = null!;
        DescriptiveStatistics.Percentile p = null!;
        var o = PropertyNameDelimiter;

        var C = converter ?? (x => x);

        return new DescriptiveStatistics
        {
            Sum = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Sum)}")),
            Count = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Count)}")),
            Min = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Min)}")),
            Max = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Max)}")),
            Mean = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Mean)}")),
            Variance = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Variance)}")),
            Skewness = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Skewness)}")),
            Kurtosis = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Kurtosis)}")),
            Percentiles = new DescriptiveStatistics.Percentile
            {
                P01 = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P01)}")),
                P05 = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P05)}")),
                P25 = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P25)}")),
                P50 = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P50)}")),
                P75 = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P75)}")),
                P95 = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P95)}")),
                P99 = C(reader.GetValue<double>($"{prefix}{o}{nameof(d.Percentiles)}{o}{nameof(p.P99)}")),
            }
        };
    }

    public static PropertyMapping<T>[] ToMappings<T>(
        Func<T, OHLCV?> getOhlcv,
        string prefix = "OHLCV")
    {
        var o = PropertyNameDelimiter;
        OHLCV x = null!;
        return new MappingBuilder<T>()
            .Map($"{prefix}{o}{nameof(x.Open)}", s => (double?)(getOhlcv(s)?.Open))
            .Map($"{prefix}{o}{nameof(x.High)}", s => (double?)(getOhlcv(s)?.High))
            .Map($"{prefix}{o}{nameof(x.Low)}", s => (double?)(getOhlcv(s)?.Low))
            .Map($"{prefix}{o}{nameof(x.Close)}", s => (double?)(getOhlcv(s)?.Close))
            .Map($"{prefix}{o}{nameof(x.Volume)}", s => getOhlcv(s)?.Volume)
            .Map($"{prefix}{o}{nameof(x.VWAP)}", s => (double?)(getOhlcv(s)?.VWAP))
            .Map($"{prefix}{o}{nameof(x.OHLC4)}", s => (double?)(getOhlcv(s)?.OHLC4))
            .ToArray();
    }

    public static OHLCV? ReadOHLCV(
        IValueReader reader,
        string prefix = "OHLCV")
    {
        var o = PropertyNameDelimiter;

        OHLCV x = null!;
        if (reader.GetValue<string>($"{prefix}{o}{nameof(x.Open)}") == null)
            return null;

        return new OHLCV(
            timestamp: 0,
            open: reader.GetValue<decimal>($"{prefix}{o}{nameof(x.Open)}"),
            high: reader.GetValue<decimal>($"{prefix}{o}{nameof(x.High)}"),
            low: reader.GetValue<decimal>($"{prefix}{o}{nameof(x.Low)}"),
            close: reader.GetValue<decimal>($"{prefix}{o}{nameof(x.Close)}"),
            volume: reader.GetValue<long>($"{prefix}{o}{nameof(x.Volume)}"),
            vwap: reader.GetValue<decimal>($"{prefix}{o}{nameof(x.VWAP)}"));
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

    public static Dictionary<TEnum, TValue> GetDictionary<TEnum, TValue>(
        IValueReader reader,
        string prefix)
        where TEnum : struct, Enum
    {
        var enumTypeName = GetLabel<TEnum>();
        var dict = new Dictionary<TEnum, TValue>();

        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            var key = GetLabel(prefix, enumTypeName, enumValue.ToString());

            if (reader.GetValue<string>(key) != null)
            {
                dict[enumValue] = reader.GetValue<TValue>(key)!;
            }
        }

        return dict;
    }

    public static Dictionary<EdgeKind, TValue> GetDictionary<TValue>(
        IValueReader reader,
        string prefix)
    {
        var dict = new Dictionary<EdgeKind, TValue>();

        foreach (var kind in Schema.EdgeKinds)
        {
            var key = GetLabel(prefix, kind);
            if (reader.GetValue<string>(key) != null)
                dict[kind] = reader.GetValue<TValue>(key)!;
        }

        return dict;
    }
}