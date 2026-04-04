using EBA.Graph.Db.Neo4jDb;
using EBA.Utilities;

namespace EBA.Graph.Bitcoin.Strategies;

public static class PropertyMappingFactory
{
    public static char PropertyDelimiter { get; } = '-';

    public static Property HeightProperty { get; } = new(nameof(Block.Height), FieldType.Long);
    public static PropertyMapping<T> Height<T>(Func<T, long> getValue, Func<Property, string>? headerOverride = null)
    {
        return new(HeightProperty, x => getValue(x), headerOverride);
    }

    public static PropertyMapping<T> ScriptSHA256HashString<T>(Func<T, string?> getValue, Func<Property, string>? headerOverride = null)
    {
        return new(nameof(ScriptNode.SHA256Hash), FieldType.String, x => getValue(x), headerOverride);
    }

    public static PropertyMapping<T> Txid<T>(Func<T, string> getValue, Func<Property, string>? headerOverride = null)
    {
        return new(nameof(TxNode.Txid), FieldType.String, x => getValue(x), headerOverride);
    }

    public static Property ValueProperty { get; } = new("Value", FieldType.Long);
    public static PropertyMapping<T> Value<T>(Func<T, long> getValue)
    {
        return new(
            ValueProperty,
            x => getValue(x),
            deserializer: v => (long)v!);

        /* you may use the following if you need to convert between Satoshi and BTC, 
         * but be aware that this will make the deserialization more complex 
         * and may lead to precision issues if not handled carefully.
         * 
        return new(
            ValueProperty, 
            x => Helpers.Satoshi2BTC(getValue(x)),
            deserializer: v => Helpers.BTC2Satoshi((double)v!));*/
    }

    public static PropertyMapping<T> SourceId<T>(string idSpace, Func<T, object?> getValue)
    {
        return new(":START_ID", FieldType.String, getValue, _ => $":START_ID({idSpace})");
    }
    public static PropertyMapping<T> TargetId<T>(string idSpace, Func<T, object?> getValue)
    {
        return new(":END_ID", FieldType.String, getValue, _ => $":END_ID({idSpace})");
    }

    public static string TypePropertyName { get; } = ":TYPE";
    public static PropertyMapping<T> EdgeType<T>(Func<T, object?> getType)
    {
        return new(TypePropertyName, FieldType.String, getType, _ => TypePropertyName);
    }

    public static PropertyMapping<T> Label<T>(object label)
    {
        return new(":LABEL", FieldType.String, _ => label, _ => ":LABEL");
    }

    public static PropertyMapping<T>[] DescriptiveStats<T>(
        string prefix,
        Func<T, DescriptiveStatistics?> getStats,
        Func<double?, double>? converter = null)
    {
        // dummy variables to help with nameof expressions
        DescriptiveStatistics d = null!;
        DescriptiveStatistics.Percentile p = null!;

        var C = converter ?? (x => x ?? double.NaN);

        return
        [
            new($"{prefix}.{nameof(d.Sum)}", FieldType.Double, s => C(getStats(s)?.Sum)),
            new($"{prefix}.{nameof(d.Count)}", FieldType.Double, s => C(getStats(s)?.Count)),
            new($"{prefix}.{nameof(d.Min)}", FieldType.Double, s => C(getStats(s)?.Min)),
            new($"{prefix}.{nameof(d.Max)}", FieldType.Double, s => C(getStats(s)?.Max)),
            new($"{prefix}.{nameof(d.Mean)}", FieldType.Double, s => C(getStats(s)?.Mean)),
            new($"{prefix}.{nameof(d.Variance)}", FieldType.Double, s => C(getStats(s)?.Variance)),
            new($"{prefix}.{nameof(d.Skewness)}", FieldType.Double, s => C(getStats(s)?.Skewness)),
            new($"{prefix}.{nameof(d.Kurtosis)}", FieldType.Double, s => C(getStats(s)?.Kurtosis)),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P01)}", FieldType.Double, s => C(getStats(s)?.Percentiles.P01)),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P05)}", FieldType.Double, s => C(getStats(s)?.Percentiles.P05)),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P25)}", FieldType.Double, s => C(getStats(s)?.Percentiles.P25)),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P50)}", FieldType.Double, s => C(getStats(s)?.Percentiles.P50)),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P75)}", FieldType.Double, s => C(getStats(s)?.Percentiles.P75)),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P95)}", FieldType.Double, s => C(getStats(s)?.Percentiles.P95)),
            new($"{prefix}.{nameof(d.Percentiles)}.{nameof(p.P99)}", FieldType.Double, s => C(getStats(s)?.Percentiles.P99)),
        ];
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
        return [..
            Enum.GetValues<ScriptType>()
                .Select(scriptType => new PropertyMapping<T>(
                    $"{prefix}.ScriptType.{scriptType}",
                    FieldType.Long,
                    x => getScriptTypeCounts(x).GetValueOrDefault(scriptType)))];
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

        // Make sure to keep EdgeKind string representation compatible with neo4j header requirements. 
        return
        [
            ..  keys.Select(k => new PropertyMapping<T>(
                $"{prefix}.{k.Source}_{k.Relation}_{k.Target}",
                FieldType.Double,
                n => C(getDict(n).TryGetValue(k, out var v) ? v : 0)))
        ];
    }

    public static PropertyMapping<T>[] DictionaryToColumns<T>(
        string prefix,
        IEnumerable<EdgeKind> keys,
        Func<T, Dictionary<EdgeKind, uint>> getDict)
    {
        return
        [
            ..  keys.Select(k => new PropertyMapping<T>(
                $"{prefix}.{EdgeKindToPropertyName(k)}",
                FieldType.Long,
                n => getDict(n).TryGetValue(k, out var v) ? v : 0))
        ];
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
        return
        [
            new($"{prefix}.{nameof(o.Open)}", FieldType.Double, s => (double?)(getOhlcv(s)?.Open)),
            new($"{prefix}.{nameof(o.High)}", FieldType.Double, s => (double?)(getOhlcv(s)?.High)),
            new($"{prefix}.{nameof(o.Low)}", FieldType.Double, s => (double?)(getOhlcv(s)?.Low)),
            new($"{prefix}.{nameof(o.Close)}", FieldType.Double, s => (double?)(getOhlcv(s)?.Close)),
            new($"{prefix}.{nameof(o.VWAP)}", FieldType.Double, s => (double?)(getOhlcv(s)?.VWAP)),
            new($"{prefix}.{nameof(o.OHLC4)}", FieldType.Double, s => (double?)(getOhlcv(s)?.OHLC4)),
            new($"{prefix}.{nameof(o.Volume)}", FieldType.Long, s => getOhlcv(s)?.Volume),
        ];
    }

    public static OHLCV? ReadOHLCV(
        IReadOnlyDictionary<string, object> properties,
        string prefix = "OHLCV")
    {
        OHLCV o = null!;
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