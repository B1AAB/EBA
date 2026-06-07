using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Utilities;
using System.IO.Compression;

namespace AAB.EBA.Blockchains.Bitcoin.Utilities;

public class UtxoEconomics(ILogger<BitcoinOrchestrator> logger)
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

    private readonly ILogger<BitcoinOrchestrator> _logger = logger;

    private readonly int _creationHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.CreationHeight);
    private readonly int _spentHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.SpentHeight);
    private readonly int _valueIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.Value);

    public async Task SetBlockOnChainEconomicsAsync(
        List<Batch> batches,
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
}
