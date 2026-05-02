using System.Globalization;

namespace AAB.EBA.Utilities;

public class OHLCV(long timestamp, decimal open, decimal high, decimal low, decimal close, long volume, decimal vwap)
{
    public long Timestamp { get; } = timestamp;
    public decimal Open { get; } = open;
    public decimal High { get; } = high;
    public decimal Low { get; } = low;
    public decimal Close { get; } = close;
    public long Volume { get; } = volume;

    /// <summary>
    /// Volume-Weighted Average Price (VWAP)
    /// </summary>
    public decimal VWAP { get; } = vwap;

    public decimal OHLC4 { get { return (Open + High + Low + Close) / 4; } }

    public decimal GetFiatValue(long satoshiAmount)
    {
        return satoshiAmount / (decimal)BitcoinChainAgent.Coin * VWAP;
    }

    public static bool TryParse(string csvLine, out OHLCV? candle)
    {
        candle = default;

        var values = csvLine.Split(',');
        if (values.Length < 6) return false;

        if (!double.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var timestamp) ||
            !decimal.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var open) ||
            !decimal.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var high) ||
            !decimal.TryParse(values[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var low) ||
            !decimal.TryParse(values[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var close) ||
            !decimal.TryParse(values[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
        {
            return false;
        }

        candle = new OHLCV(
            timestamp: (long)timestamp,
            open: open,
            high: high,
            low: low,
            close: close,
            volume: (long)(volume * BitcoinChainAgent.Coin),
            vwap: 0);

        return true;
    }

    public static decimal GetVWAP(List<OHLCV> candles)
    {
        decimal sumVolume = 0;
        decimal vwapSum = 0;

        foreach (var c in candles)
        {
            sumVolume += c.Volume;
            vwapSum += c.OHLC4 * c.Volume;
        }

        if (sumVolume == 0)
            return 0;

        return vwapSum / sumVolume;
    }

    public static bool TryParseFile(string filename, out Dictionary<long, OHLCV> candles)
    {
        candles = [];

        using var reader = new StreamReader(filename);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var cols = line.Split('\t');
            if (cols.Length < 8) return false;

            if (!long.TryParse(cols[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var height) ||
                !long.TryParse(cols[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var timestamp) ||
                !decimal.TryParse(cols[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var open) ||
                !decimal.TryParse(cols[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var high) ||
                !decimal.TryParse(cols[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var low) ||
                !decimal.TryParse(cols[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var close) ||
                !decimal.TryParse(cols[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var vwap) ||
                !decimal.TryParse(cols[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var ohlc4) ||
                !long.TryParse(cols[8], NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
            {
                return false;
            }

            candles[height] = new OHLCV(
                timestamp: timestamp,
                open: open,
                high: high,
                low: low,
                close: close,
                volume: volume,
                vwap: vwap);
        }
        return true;
    }

    public static string[] GetFeaturesName()
    {
        return
        [
            nameof(Timestamp),
            nameof(Open),
            nameof(High),
            nameof(Low),
            nameof(Close),
            nameof(VWAP),
            nameof(OHLC4),
            $"{nameof(Volume)}(Satoshi)"
        ];
    }

    public string[] GetFeatures()
    {
        return
        [
            Timestamp.ToString(CultureInfo.InvariantCulture),
            Open.ToString(CultureInfo.InvariantCulture),
            High.ToString(CultureInfo.InvariantCulture),
            Low.ToString(CultureInfo.InvariantCulture),
            Close.ToString(CultureInfo.InvariantCulture),
            VWAP.ToString(CultureInfo.InvariantCulture),
            OHLC4.ToString(CultureInfo.InvariantCulture),
            Volume.ToString(CultureInfo.InvariantCulture)
        ];
    }
}
