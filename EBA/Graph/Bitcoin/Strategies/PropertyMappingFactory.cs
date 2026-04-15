using EBA.Utilities;

namespace EBA.Graph.Bitcoin.Strategies;

public static class PropertyMappingFactory
{
    public static PropertyMapping<T>[] DescriptiveStats<T>(
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

    public static PropertyMapping<T>[] ScriptTypeCounts<T>(
        string prefix,
        Func<T, Dictionary<ScriptType, long>> getScriptTypeCounts)
    {
        var builder = new MappingBuilder<T>();
        
        foreach (var scriptType in Enum.GetValues<ScriptType>())
        {
            builder.Map(
                $"{prefix}.ScriptType.{scriptType}", 
                x => getScriptTypeCounts(x).GetValueOrDefault(scriptType));
        }

        return builder.ToArray();
    }

    public static Dictionary<ScriptType, long> ReadScriptTypeCounts(
        string prefix,
        IReadOnlyDictionary<string, object> properties)
    {
        return Enum.GetValues<ScriptType>()
            .ToDictionary(
                scriptType => scriptType,
                scriptType => (long)properties[$"{prefix}.ScriptType.{scriptType}"]);
    }

    public static PropertyMapping<T>[] DictionaryToColumns<T>(
        string prefix,
        IEnumerable<EdgeKind> keys,
        Func<T, Dictionary<EdgeKind, long>> getDict,
        Func<double?, double>? converter = null)
    {
        var C = converter ?? (x => x ?? double.NaN);
        var builder = new MappingBuilder<T>();

        // Make sure to keep EdgeKind string representation compatible with neo4j header requirements. 
        foreach (var k in keys)
        {
            builder.Map(
                $"{prefix}.{k.Source}_{k.Relation}_{k.Target}", 
                n => C(getDict(n).TryGetValue(k, out var v) ? v : 0));
        }

        return builder.ToArray();
    }

    public static PropertyMapping<T>[] DictionaryToColumns<T>(
        string prefix,
        IEnumerable<EdgeKind> keys,
        Func<T, Dictionary<EdgeKind, uint>> getDict)
    {
        var builder = new MappingBuilder<T>();
        
        foreach (var k in keys)
        {
            builder.Map(
                $"{prefix}.{EdgeKindToPropertyName(k)}", 
                n => getDict(n).TryGetValue(k, out var v) ? v : 0U);
        }
        
        return builder.ToArray();
    }

    public static Dictionary<EdgeKind, TValue> ReadDictionary<TValue>(
        string prefix,
        IEnumerable<EdgeKind> keys,
        IReadOnlyDictionary<string, object> properties)
    {
        var result = new Dictionary<EdgeKind, TValue>();
        foreach (var key in keys)
            if (properties.TryGetValue($"{prefix}.{EdgeKindToPropertyName(key)}", out var val) && val is IConvertible convertible)
                result[key] = (TValue)convertible.ToType(typeof(TValue), null);

        return result;
    }

    private static string EdgeKindToPropertyName(EdgeKind edgeKind)
    {
        // Make sure to keep EdgeKind string representation compatible with neo4j header requirements. 
        return $"{edgeKind.Source}_{edgeKind.Relation}_{edgeKind.Target}";
    }


    public static PropertyMapping<T>[] OHLCV<T>(
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
}