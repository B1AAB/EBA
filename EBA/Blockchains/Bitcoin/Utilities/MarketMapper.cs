using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.Utilities;

public class MarketMapper(BitcoinChainAgent agent, ILogger<BitcoinOrchestrator> logger)
{
    private readonly BitcoinChainAgent _agent = agent;
    private readonly ILogger<BitcoinOrchestrator> _logger = logger;

    private record BlockOHLC(BlockMetadata Metadata, OHLCV ohlcv);

    public async Task MapAsync(Options options, CancellationToken cT)
    {
        var chainInfo = await _agent.AssertChainAsync(cT);
        var blocks = await _agent.GetBlockMetadataAsync(0, chainInfo.Blocks, cT);

        var matchedBlockMarket = MatchBlockAndMarketData(blocks, options.Bitcoin.MapMarket.OhlcvSourceFilename);

        using var writer = new StreamWriter(options.Bitcoin.MapMarket.BlockOhlcvMappedFilename);
        writer.WriteLine(string.Join('\t', ["Height", .. OHLCV.GetFeaturesName()]));

        foreach (var x in matchedBlockMarket)
            writer.WriteLine(string.Join('\t', [x.Metadata.Height.ToString(), .. x.ohlcv.GetFeatures()]));

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

        for (int i = 1; i < sortedBlocks.Count; i++)
        {
            var startTime = sortedBlocks[i - 1].MedianTime;
            var endTime = sortedBlocks[i].MedianTime;

            var data = new List<OHLCV>();

            while ((line = reader.ReadLine()) != null)
            {
                if (!OHLCV.TryParse(line, out var candle) || candle == null)
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
                            timestamp: sortedBlocks[i].MedianTime,
                            open: data.First().Open,
                            high: data.Max(x => x.High),
                            low: data.Min(x => x.Low),
                            close: data.Last().Close,
                            volume: (long)Math.Round(data.Average(x => x.Volume)),
                            vwap: OHLCV.GetVWAP(data))));
            }
        }

        _logger.LogInformation(
            "Finished matching block metadata with market data. " +
            "Total matched blocks: {Count:n0}",
            matchedBlockMarket.Count);

        return matchedBlockMarket;
    }
}
