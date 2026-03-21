using System.Globalization;

namespace EBA.Blockchains.Bitcoin.Utilities;

public class MarketMapper(BitcoinChainAgent agent, ILogger<BitcoinOrchestrator> logger)
{
    private readonly BitcoinChainAgent _agent = agent;
    private readonly ILogger<BitcoinOrchestrator> _logger = logger;

    private record OHLCV(long Timestamp, float Open, float High, float Low, float Close, float Volume)
    {
        public static bool TryParse(string csvLine, out OHLCV candle)
        {
            candle = null;

            var values = csvLine.Split(',');
            if (values.Length < 6) return false;

            if (!double.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var ts) ||
                !float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var open) ||
                !float.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var high) ||
                !float.TryParse(values[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var low) ||
                !float.TryParse(values[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var close) ||
                !float.TryParse(values[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
            {
                return false;
            }

            candle = new OHLCV(
                Timestamp: (long)ts,
                Open: open,
                High: high,
                Low: low,
                Close: close,
                Volume: volume
            );

            return true;
        }
    }

    private record BlockOHLC(BlockMetadata Metadata, OHLCV OHLC);

    public async Task MapAsync(Options options, CancellationToken cT)
    {
        var chainInfo = await _agent.AssertChainAsync(cT);
        var blocks = await _agent.GetBlockMetadataAsync(0, chainInfo.Blocks, cT);

        var matchedBlockMarket = MatchBlockAndMarketData(blocks, options.Bitcoin.MapMarket.OhlcvSourceFilename);

        using var writer = new StreamWriter(options.Bitcoin.MapMarket.BlockOhlcvMappedFilename);
        foreach (var x in matchedBlockMarket)
            writer.WriteLine(
                string.Join(
                    '\t',
                    x.Metadata.Height,
                    x.Metadata.MedianTime,
                    x.OHLC.Open,
                    x.OHLC.High,
                    x.OHLC.Low,
                    x.OHLC.Close,
                    x.OHLC.Volume));

        _logger.LogInformation(
            "Finished writing mapped block and market data to {MappedOutputFilename}",
            options.Bitcoin.MapMarket.BlockOhlcvMappedFilename);
    }

    private List<BlockOHLC> MatchBlockAndMarketData(List<BlockMetadata> blocks, string marketDataFilename)
    {
        var matchedBlockMarket = new List<BlockOHLC>();

        var sortedBlocks = blocks.OrderBy(b => b.Height).ToList();

        using var reader = new StreamReader(marketDataFilename);

        var line = reader.ReadLine();

        _logger.LogInformation(
            "Matching block metadata with market data from {MarketDataFilename}",
            marketDataFilename);

        for (int i = 0; i < sortedBlocks.Count - 1; i++)
        {
            var startTime = sortedBlocks[i].MedianTime;
            var endTime = sortedBlocks[i + 1].MedianTime;

            var data = new List<OHLCV>();

            while ((line = reader.ReadLine()) != null)
            {
                if (!OHLCV.TryParse(line, out var candle))
                    continue;

                if (candle.Timestamp < startTime)
                    continue;

                if (candle.Timestamp >= endTime)
                    break;

                data.Add(candle);
            }

            if (data.Count != 0)
            {
                matchedBlockMarket.Add(
                    new BlockOHLC(
                        sortedBlocks[i],
                        new OHLCV(
                            sortedBlocks[i].MedianTime,
                            data.First().Open,
                            data.Max(x => x.High),
                            data.Min(x => x.Low),
                            data.Last().Close,
                            data.Average(x => x.Volume))));
            }
        }

        _logger.LogInformation(
            "Finished matching block metadata with market data. " +
            "Total matched blocks: {Count:n0}",
            matchedBlockMarket.Count);

        return matchedBlockMarket;
    }
}
