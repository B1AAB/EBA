using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Utilities;
using System.IO.Compression;

namespace AAB.EBA.Blockchains.Bitcoin.Utilities;

public class UtxoEconomics(ILogger<BitcoinOrchestrator> logger)
{
    private readonly ILogger<BitcoinOrchestrator> _logger = logger;

    private readonly int _creationHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.CreationHeight);
    private readonly int _spentHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.SpentHeight);
    private readonly int _valueIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.Value);

    private long edgesProcessedPerChunkCount = 0;
    private long skippedEdgesPerChunkCount = 0;
    private long chunkUtxoId = 0;
    private int batchCounter = 0;
    private readonly ConcurrentDictionary<long, BlockUtxoDelta> utxoDeltaByHeight = new();

    private record struct MinimalUtxo(long Id, decimal FiatValue);

    /// <summary>
    /// Holds a set of Utxo events in a block:
    /// Utxos created and spent in the block, 
    /// where each Utxo event is represented by a minimal set of properties. 
    /// </summary>
    private class BlockUtxoDelta
    {
        public ConcurrentBag<MinimalUtxo> UtxosCreated { get; } = [];
        public ConcurrentBag<long> UtxosSpent { get; } = [];
    }

    public async Task SetBlockOnChainEconomics(
        List<Batch> batches,
        SortedDictionary<long, BlockNode> blockNodes,
        Dictionary<long, OHLCV> ohlcv,
        CancellationToken ct,
        int batchesPerChunk = 100)
    {
        var sortedHeights = blockNodes.Keys.ToArray();

        long totalEdgesProcessed = 0;
        long totalSkippedEdges = 0;

        foreach (var batchChunk in batches.Chunk(batchesPerChunk))
        {
            utxoDeltaByHeight.Clear();
            chunkUtxoId = 0;

            totalEdgesProcessed += edgesProcessedPerChunkCount;
            edgesProcessedPerChunkCount = 0;

            totalSkippedEdges += skippedEdgesPerChunkCount;
            skippedEdgesPerChunkCount = 0;

            await Parallel.ForEachAsync(batchChunk, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            },
            async (batch, ct) =>
            {
                await ReadUtxoLifecycle(batch, ohlcv, ct);
                Interlocked.Increment(ref batchCounter);

                if (batchCounter % 100 == 0)
                {
                    _logger.LogInformation(
                        "Read Utxo lifecycle from {batchCount} batches. Total edges processed: {edgesProcessed}, Total edges skipped: {edgesSkipped}",
                        batchCounter, edgesProcessedPerChunkCount, skippedEdgesPerChunkCount);
                }
            });

            _logger.LogInformation(
                "Apply market indicators from Utxo lifecycle for chunk of {batchCount} batches. " +
                "Total edges processed in chunk: {edgesProcessed}, Total edges skipped in chunk: {edgesSkipped}",
                batchChunk.Count(), edgesProcessedPerChunkCount, skippedEdgesPerChunkCount);
            ApplyMarketIndicatorsFromUtxoLifecycle(sortedHeights, blockNodes);

            _logger.LogInformation(
                "Completed processing chunk of {batchCount} batches. " +
                "Total edges processed so far: {edgesProcessed}, Total edges skipped so far: {edgesSkipped}",
                batchChunk.Count(), totalEdgesProcessed, totalSkippedEdges);
        }

        _logger.LogInformation(
            "Completed processing all batches. Total edges processed: {edgesProcessed}, Total edges skipped: {edgesSkipped}",
            totalEdgesProcessed, totalSkippedEdges);
    }

    private async Task ReadUtxoLifecycle(Batch batch, Dictionary<long, OHLCV> ohlcv, CancellationToken ct)
    {
        using var reader = new StreamReader(
            new GZipStream(
                File.OpenRead(batch.GetFilename(T2SEdge.Kind)),
                CompressionMode.Decompress));

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            var cols = line.Split(Options.CsvDelimiter);

            var creationHeight = long.Parse(cols[_creationHeightIdx]);
            var spentHeight = long.Parse(cols[_spentHeightIdx]);
            var value = long.Parse(cols[_valueIdx]);

            if (!ohlcv.TryGetValue(creationHeight, out var creationBlockOHLCV))
            {
                Interlocked.Increment(ref skippedEdgesPerChunkCount);
                continue;
            }

            var utxoFiatValueAtCreation = creationBlockOHLCV.GetFiatValue(value);
            if (utxoFiatValueAtCreation > 0)
            {
                var utxoId = Interlocked.Increment(ref chunkUtxoId);

                // utxo deltas at the creation height
                utxoDeltaByHeight.GetOrAdd(creationHeight, _ => new BlockUtxoDelta())
                    .UtxosCreated.Add(new MinimalUtxo(utxoId, utxoFiatValueAtCreation));

                // utxo deltas at the spent height
                utxoDeltaByHeight.GetOrAdd(spentHeight, _ => new BlockUtxoDelta())
                    .UtxosSpent.Add(utxoId);
            }

            Interlocked.Increment(ref edgesProcessedPerChunkCount);

            if (edgesProcessedPerChunkCount % 100_000 == 0)
            {
                _logger.LogInformation(
                    "Processed {edgesProcessed} edges in current chunk. Total edges skipped in current chunk: {edgesSkipped}",
                    edgesProcessedPerChunkCount, skippedEdgesPerChunkCount);
            }
        }
    }

    private void ApplyMarketIndicatorsFromUtxoLifecycle(long[] sortedHeights, SortedDictionary<long, BlockNode> blockNodes)
    {
        var activeUtxos = new Dictionary<long, decimal>();
        decimal runningRealizedCap = 0;

        foreach (var h in sortedHeights)
        {
            var block = blockNodes[h];

            if (utxoDeltaByHeight.TryGetValue(h, out var blockUtxoChanges))
            {
                foreach (var utxoChange in blockUtxoChanges.UtxosCreated)
                {
                    activeUtxos[utxoChange.Id] = utxoChange.FiatValue;
                    runningRealizedCap += utxoChange.FiatValue;
                }

                foreach (var id in blockUtxoChanges.UtxosSpent)
                {
                    if (activeUtxos.Remove(id, out var removedValue))
                        runningRealizedCap -= removedValue;
                }
            }

            block.BlockMetadata.RealizedCap ??= 0;
            block.BlockMetadata.RealizedCap += runningRealizedCap;

            if (block.BlockMetadata.Ohlcv != null && activeUtxos.Count > 0)
            {
                var vwap = block.BlockMetadata.Ohlcv.VWAP;
                decimal totalLoss = 0;
                decimal totalProfit = 0;

                foreach (var fiatValue in activeUtxos.Values)
                {
                    if (fiatValue < vwap)
                        totalLoss += vwap - fiatValue;
                    else
                        totalProfit += fiatValue - vwap;
                }

                if (totalLoss > 0)
                {
                    block.BlockMetadata.UnrealizedLoss ??= 0;
                    block.BlockMetadata.UnrealizedLoss += totalLoss;
                }

                if (totalProfit > 0)
                {
                    block.BlockMetadata.UnrealizedProfit ??= 0;
                    block.BlockMetadata.UnrealizedProfit += totalProfit;
                }
            }
        }
    }
}
