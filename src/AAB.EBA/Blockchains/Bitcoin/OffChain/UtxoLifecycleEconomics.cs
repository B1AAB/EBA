using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Utilities;
using System.IO.Compression;

namespace AAB.EBA.Blockchains.Bitcoin.OffChain;

public class EconomicAugmentor(ILogger<BitcoinOrchestrator> logger, Options options)
{
    private class BatchUtxoDeltas(int maximumBlockHeight)
    {
        public readonly long[] CreatedSatoshis = new long[maximumBlockHeight + 1];
        public readonly Dictionary<int, Dictionary<int, long>> SpentSatoshis = [];
        public long EdgesProcessed;
    }

    private class ActiveUtxoSet(int maxBlockHeight)
    {
        public readonly long[] UnspentSatoshis = new long[maxBlockHeight + 1];
        public readonly HashSet<int> ActiveCreationHeights = [];
        public decimal RealizedCap { get; private set; }

        public void AddCreatedSatoshis(int height, long satoshis, double fiatPrice)
        {
            if (satoshis <= 0)
                return;

            UnspentSatoshis[height] += satoshis;
            RealizedCap += (decimal)(satoshis * fiatPrice);
            ActiveCreationHeights.Add(height);
        }

        public void RemoveSpentSatoshis(int creationHeight, long satoshis, double fiatPrice)
        {
            UnspentSatoshis[creationHeight] -= satoshis;
            RealizedCap -= (decimal)(satoshis * fiatPrice);

            if (UnspentSatoshis[creationHeight] == 0)
                ActiveCreationHeights.Remove(creationHeight);
        }
    }

    private readonly Options _options = options;
    private readonly ILogger<BitcoinOrchestrator> _logger = logger;

    private readonly int _creationHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.CreationHeight);
    private readonly int _spentHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.SpentHeight);
    private readonly int _valueIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.Value);

    public async Task SetBlockMarketIndicatorsAsync(CancellationToken ct)
    {
        var batches = await Batch.DeserializeBatchesAsync(_options.Bitcoin.MapSpends.BatchesFilename);
        _logger.LogInformation(
            "Deserialized {n:n0} batches from {filename}.", 
            batches.Count, _options.Bitcoin.MapSpends.BatchesFilename);

        var (_, blockNodes) = await Utilities.BitcoinHelpers.GetHeightToBatchMapping(batches, _logger, ct);

        var blockOhlcvMapping = await SetBlockOHLCV(blockNodes, ct);

        await SetBlockValuationMetrics(batches, blockNodes, blockOhlcvMapping, ct);

        ComputeThermocap(blockNodes, blockOhlcvMapping);

        await SaveBlockNodesWithUpdatedEconomicIndicators(batches, blockNodes, ct);
    }

    private async Task SetBlockValuationMetrics(List<Batch> batches,
        SortedDictionary<long, BlockNode> blockNodes,
        Dictionary<long, OHLCV> ohlcv,
        CancellationToken ct)
    {
        var sortedHeights = blockNodes.Keys.ToArray();
        int maxBlockHeight = (int)(sortedHeights.Length > 0 ? sortedHeights.Max() : 0);

        double[] satoshiFiatValueByHeight = GetSatoshiFiatPriceByBlockHeight(ohlcv, maxBlockHeight);

        var (createdSatoshisByHeight, spentSatoshisByHeight) = await AggregateUtxoDeltasAsync(
            batches,
            satoshiFiatValueByHeight,
            maxBlockHeight,
            ct);

        EvaluateChainStateEconomics(
            blockNodes,
            createdSatoshisByHeight,
            spentSatoshisByHeight,
            satoshiFiatValueByHeight,
            maxBlockHeight);
    }

    private static double[] GetSatoshiFiatPriceByBlockHeight(Dictionary<long, OHLCV> ohlcv, int maximumBlockHeight)
    {
        var fiatPrices = new double[maximumBlockHeight + 1];

        for (var height = 0; height <= maximumBlockHeight; height++)
            if (ohlcv.TryGetValue(height, out var blockPrice))
                fiatPrices[height] = (double)blockPrice.GetFiatValue(1);

        return fiatPrices;
    }

    private async Task<(long[] CreatedSatoshis, Dictionary<int, long>[] SpentSatoshis)> AggregateUtxoDeltasAsync(
        List<Batch> batches,
        double[] satoshiFiatValueByHeight,
        int maxBlockHeight,
        CancellationToken ct)
    {
        long totalEdgesProcessed = 0;
        var totalCreatedSatoshisByHeight = new long[maxBlockHeight + 1];
        var totalSpentSatoshisByHeight = new Dictionary<int, long>[maxBlockHeight + 1];
        object dictionaryMergeLock = new();

        var batchCounter = 0;

        var options = new ParallelOptions
        {
            #if DEBUG
            MaxDegreeOfParallelism = 1,
            #endif
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(batches, parallelOptions: options, async (batch, _ct) =>
        {
            var batchDeltas = await ParseBatchUtxoDeltasAsync(
                batch,
                satoshiFiatValueByHeight,
                maxBlockHeight,
                _ct);

            Interlocked.Add(ref totalEdgesProcessed, batchDeltas.EdgesProcessed);

            lock (dictionaryMergeLock)
            {
                MergeBatchDeltasIntoTotalState(
                    batchDeltas,
                    totalCreatedSatoshisByHeight,
                    totalSpentSatoshisByHeight,
                    maxBlockHeight);
            }

            Interlocked.Increment(ref batchCounter);
            if (batchCounter % 100 == 0)
            {
                _logger.LogInformation(
                    "Read and merged UTxO deltas in {batchCount:n0}/{totalBatches:n0} batches. Total UTxO edges processed so far: {edgeCount:n0}.",
                    batchCounter, batches.Count, totalEdgesProcessed);
            }
        });

        _logger.LogInformation("Successfully aggregated {edgeCount:n0} UTxO edges.", totalEdgesProcessed);

        return (totalCreatedSatoshisByHeight, totalSpentSatoshisByHeight);
    }

    private async Task<BatchUtxoDeltas> ParseBatchUtxoDeltasAsync(
        Batch batch,
        double[] fiatPricePerSatoshi,
        int maxBlockHeight,
        CancellationToken ct)
    {
        var deltas = new BatchUtxoDeltas(maxBlockHeight);

        using var reader = new StreamReader(new GZipStream(
            File.OpenRead(batch.GetFilename(T2SEdge.Kind)),
            CompressionMode.Decompress));

        string? line;

        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            var columns = line.Split(Options.CsvDelimiter);

            var creationHeight = int.Parse(columns[_creationHeightIdx]);
            var rawSpentHeight = long.Parse(columns[_spentHeightIdx]);
            var satoshis = long.Parse(columns[_valueIdx]);

            if (creationHeight > maxBlockHeight || fiatPricePerSatoshi[creationHeight] <= 0)
                continue;

            deltas.CreatedSatoshis[creationHeight] += satoshis;

            if (rawSpentHeight <= maxBlockHeight) // utxo is spent or not yet?
            {
                var spentHeight = (int)rawSpentHeight;

                if (!deltas.SpentSatoshis.TryGetValue(spentHeight, out var spendsAtHeight))
                {
                    spendsAtHeight = [];
                    deltas.SpentSatoshis[spentHeight] = spendsAtHeight;
                }

                if (!spendsAtHeight.TryGetValue(creationHeight, out var existingSatoshis))
                    spendsAtHeight[creationHeight] = satoshis;
                else
                    spendsAtHeight[creationHeight] = existingSatoshis + satoshis;
            }

            deltas.EdgesProcessed++;
        }

        return deltas;
    }

    private static void MergeBatchDeltasIntoTotalState(
        BatchUtxoDeltas batchDeltas,
        long[] totalCreatedSatoshisByHeight,
        Dictionary<int, long>[] totalSpentSatoshisByHeight,
        int maxBlockHeight)
    {
        for (var h = 0; h <= maxBlockHeight; h++)
            totalCreatedSatoshisByHeight[h] += batchDeltas.CreatedSatoshis[h];

        foreach (var spentKVP in batchDeltas.SpentSatoshis)
        {
            var spentHeight = spentKVP.Key;
            totalSpentSatoshisByHeight[spentHeight] ??= [];

            foreach (var creationKVP in spentKVP.Value)
            {
                var creationHeight = creationKVP.Key;
                var satoshisToMerge = creationKVP.Value;

                if (totalSpentSatoshisByHeight[spentHeight].TryGetValue(creationHeight, out var existingSatoshis))
                    totalSpentSatoshisByHeight[spentHeight][creationHeight] = existingSatoshis + satoshisToMerge;
                else
                    totalSpentSatoshisByHeight[spentHeight][creationHeight] = satoshisToMerge;
            }
        }
    }

    private void EvaluateChainStateEconomics(
        SortedDictionary<long, BlockNode> blockNodes,
        long[] createdSatoshisByHeight,
        Dictionary<int, long>[] spentSatoshisByHeight,
        double[] satoshiFiatValueByHeight,
        int maxBlockHeight)
    {
        var activeUtxoSet = new ActiveUtxoSet(maxBlockHeight);

        for (var currentBlockHeight = 0; currentBlockHeight <= maxBlockHeight; currentBlockHeight++)
        {
            if (!blockNodes.TryGetValue(currentBlockHeight, out var blockNode))
                continue;

            var newlyCreatedSatoshis = createdSatoshisByHeight[currentBlockHeight];
            activeUtxoSet.AddCreatedSatoshis(
                currentBlockHeight,
                newlyCreatedSatoshis,
                satoshiFiatValueByHeight[currentBlockHeight]);

            if (spentSatoshisByHeight[currentBlockHeight] != null)
            {
                foreach (var spendEvent in spentSatoshisByHeight[currentBlockHeight])
                {
                    var creationHeight = spendEvent.Key;
                    var spentSatoshis = spendEvent.Value;

                    activeUtxoSet.RemoveSpentSatoshis(
                        creationHeight,
                        spentSatoshis,
                        satoshiFiatValueByHeight[creationHeight]);
                }
            }

            blockNode.BlockMetadata.RealizedCap = activeUtxoSet.RealizedCap;

            SetUnrealizedProfitAndLoss(blockNode, activeUtxoSet, satoshiFiatValueByHeight);

            if (currentBlockHeight > 0 && currentBlockHeight % 100_000 == 0)
            {
                _logger.LogInformation("Evaluated chain state up to block {height:n0}.", currentBlockHeight);
            }
        }
    }

    private static void SetUnrealizedProfitAndLoss(
        BlockNode blockNode,
        ActiveUtxoSet activeUtxoSet,
        double[] fiatPricePerSatoshi)
    {
        if (blockNode.BlockMetadata.Ohlcv == null || activeUtxoSet.ActiveCreationHeights.Count == 0)
        {
            blockNode.BlockMetadata.UnrealizedLoss = 0m;
            blockNode.BlockMetadata.UnrealizedProfit = 0m;
            return;
        }

        var currentFiatPrice = (double)blockNode.BlockMetadata.Ohlcv.VWAP;
        double totalUnrealizedLoss = 0;
        double totalUnrealizedProfit = 0;

        foreach (var creationHeight in activeUtxoSet.ActiveCreationHeights)
        {
            var unspentSatoshis = activeUtxoSet.UnspentSatoshis[creationHeight];

            var currentFiatValue = unspentSatoshis * currentFiatPrice;
            var creationFiatValue = unspentSatoshis * fiatPricePerSatoshi[creationHeight];

            if (currentFiatValue > creationFiatValue)
                totalUnrealizedProfit += currentFiatValue - creationFiatValue;
            else
                totalUnrealizedLoss += creationFiatValue - currentFiatValue;
        }

        blockNode.BlockMetadata.UnrealizedLoss = totalUnrealizedLoss > 0 ? (decimal)totalUnrealizedLoss : 0m;
        blockNode.BlockMetadata.UnrealizedProfit = totalUnrealizedProfit > 0 ? (decimal)totalUnrealizedProfit : 0m;
    }

    private void ComputeThermocap(
        SortedDictionary<long, BlockNode> blockNodes,
        Dictionary<long, OHLCV> blockOHLCVMapping)
    {
        _logger.LogInformation("Computing thermocap for blocks.");

        foreach (var block in blockNodes)
        {
            if (block.Key == 0)
                continue;

            if (blockOHLCVMapping.TryGetValue(block.Value.BlockMetadata.Height, out var ohlcv))
            {
                var prevThermocap = blockNodes[block.Key - 1].BlockMetadata.Thermocap ?? 0;
                block.Value.BlockMetadata.Thermocap =
                    prevThermocap +
                    ohlcv.GetFiatValue(block.Value.TripletTypeValueSum[C2TEdge.Kind]);
            }
        }

        _logger.LogInformation("Completed computing thermocap for blocks.");
    }


    private async Task<Dictionary<long, OHLCV>> SetBlockOHLCV(SortedDictionary<long, BlockNode> blockNodes, CancellationToken ct)
    {
        if (!OHLCV.TryParseFile(_options.Bitcoin.Augmentor.BlockOhlcvMappedFilename, out var blockOhlcvMapping))
        {
            _logger.LogError(
                "Failed to parse OHLCV data from file: {filename}",
                _options.Bitcoin.Augmentor.BlockOhlcvMappedFilename);
            return [];
        }
        else
        {
            _logger.LogInformation(
                "Successfully parsed OHLCV data for {count:n0} blocks from file: {filename}",
                blockOhlcvMapping.Count,
                _options.Bitcoin.Augmentor.BlockOhlcvMappedFilename);
        }

        ct.ThrowIfCancellationRequested();

        var counter = 0;
        foreach (var block in blockNodes)
        {
            if (blockOhlcvMapping.TryGetValue(block.Key, out var ohlcv))
            {
                counter++;
                block.Value.BlockMetadata.Ohlcv = ohlcv;
            }
        }

        _logger.LogInformation(
            "Extended {n:n0}/{total:n0} block nodes with OHLCV data; did not find mapping OHLCV for {d:n0} block nodes.",
            counter, blockNodes.Count, blockNodes.Count - counter);

        return blockOhlcvMapping;
    }

    private async Task SaveBlockNodesWithUpdatedEconomicIndicators(List<Batch> batches, SortedDictionary<long, BlockNode> blockNodes, CancellationToken ct)
    {
        var prefix = BlockNodeDescriptor.OhlcvPropNamePrefix;
        var mapper = BlockNodeDescriptor.StaticMapper;
        var heightIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Height);
        var oOpenIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Ohlcv.Open, propertyNamePrefix: prefix);
        var oCloseIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Ohlcv.Close, propertyNamePrefix: prefix);
        var oHighIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Ohlcv.High, propertyNamePrefix: prefix);
        var oLowIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Ohlcv.Low, propertyNamePrefix: prefix);
        var oHlc4Idx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Ohlcv.OHLC4, propertyNamePrefix: prefix);
        var oVolumeIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Ohlcv.Volume, propertyNamePrefix: prefix);
        var oVwapIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Ohlcv.VWAP, propertyNamePrefix: prefix);

        var realizedCapIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.RealizedCap);
        var unrealizedProfitIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.UnrealizedProfit);
        var unrealizedLossIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.UnrealizedLoss);

        var marketCapIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.MarketCap);
        var nuplIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.NUPL);
        var nulIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.NUL);
        var nupIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.NUP);
        var mvrvIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.MVRV);
        var thermoIdx = mapper.GetPropertyCsvIndex(x => x.BlockMetadata.Thermocap);

        await Parallel.ForEachAsync(batches, new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = ct, }, async (batch, ct) =>
        {
            using var reader = new StreamReader(new GZipStream(
                File.OpenRead(batch.GetFilename(BlockNode.Kind)),
                CompressionMode.Decompress));

            using var writer = new StreamWriter(new GZipStream(
                File.Create(Helpers.AddPostfixToFilename(batch.GetFilename(BlockNode.Kind), "_with_economic_indicators")),
                CompressionLevel.Optimal));

            string? line;

            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(Options.CsvDelimiter);
                var height = long.Parse(cols[heightIdx]);
                var blockNode = blockNodes[height];
                if (blockNode.BlockMetadata.Ohlcv != null)
                {
                    cols[oOpenIdx] = blockNode.BlockMetadata.Ohlcv.Open.ToString();
                    cols[oCloseIdx] = blockNode.BlockMetadata.Ohlcv.Close.ToString();
                    cols[oHighIdx] = blockNode.BlockMetadata.Ohlcv.High.ToString();
                    cols[oLowIdx] = blockNode.BlockMetadata.Ohlcv.Low.ToString();
                    cols[oHlc4Idx] = blockNode.BlockMetadata.Ohlcv.OHLC4.ToString();
                    cols[oVolumeIdx] = blockNode.BlockMetadata.Ohlcv.Volume.ToString();
                    cols[oVwapIdx] = blockNode.BlockMetadata.Ohlcv.VWAP.ToString();

                    cols[realizedCapIdx] = blockNode.BlockMetadata.RealizedCap?.ToString() ?? "0";
                    cols[unrealizedProfitIdx] = blockNode.BlockMetadata.UnrealizedProfit?.ToString() ?? "0";
                    cols[unrealizedLossIdx] = blockNode.BlockMetadata.UnrealizedLoss?.ToString() ?? "0";

                    cols[marketCapIdx] = blockNode.BlockMetadata.MarketCap?.ToString() ?? "0";
                    cols[nuplIdx] = blockNode.BlockMetadata.NUPL?.ToString() ?? "0";
                    cols[nulIdx] = blockNode.BlockMetadata.NUL?.ToString() ?? "0";
                    cols[nupIdx] = blockNode.BlockMetadata.NUP?.ToString() ?? "0";
                    cols[mvrvIdx] = blockNode.BlockMetadata.MVRV?.ToString() ?? "0";
                    cols[thermoIdx] = blockNode.BlockMetadata.Thermocap?.ToString() ?? "0";
                }

                await writer.WriteLineAsync(string.Join(Options.CsvDelimiter, cols));
            }
        });
    }
}
