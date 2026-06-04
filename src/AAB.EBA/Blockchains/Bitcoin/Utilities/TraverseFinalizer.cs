using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Utilities;
using System.IO.Compression;

namespace AAB.EBA.Blockchains.Bitcoin.Utilities;

public class TraverseFinalizer(ILogger<BitcoinOrchestrator> logger, Options options)
{
    private readonly Options _options = options;
    private readonly ILogger<BitcoinOrchestrator> _logger = logger;

    /// <summary>
    /// Used for logging purpose, to indicate which major step is currently being processed. 
    /// </summary>
    private string _processStep = "";

    public async Task UpdatePostTraverse(CancellationToken ct)
    {
        var batches = await Batch.DeserializeBatchesAsync(_options.Bitcoin.MapSpends.BatchesFilename);
        _logger.LogInformation("Deserialized {n:n0} batches from {filename}.", batches.Count, _options.Bitcoin.MapSpends.BatchesFilename);

        _processStep = "[1/4] Block-to-batch mapping:";
        GetHeightToBatchMapping(_options, batches, out var blockToBatch, out var blockNodes);

        _processStep = "[2/4] Collecting Txo spending:";
        await CreatePerBatchSpentUtxo(batches, blockToBatch, ct);

        _processStep = "[3/4] Setting UTxO spending:";
        await SetTxoSpentHeight(batches, ct);

        _processStep = "[4/4] Setting supply amount:";
        await SetSupplyAmount(batches, blockNodes, ct);
    }

    private async Task<(Dictionary<long, Batch>, SortedDictionary<long, BlockNode>)> GetHeightToBatchMapping(
        List<Batch> batches,
        CancellationToken ct)
    {
        _logger.LogInformation("{s} Reading Block node files to create block-to-batch mapping.", _processStep);

        var concBlockToBatch = new ConcurrentDictionary<long, Batch>();
        var concBlockNodes = new ConcurrentDictionary<long, BlockNode>();

        int counter = 0;

        await Parallel.ForEachAsync(batches, ct, async (batch, ct) =>
        {
            var blockNodesFilename = batch.GetFilename(BlockNode.Kind);

            await foreach (var cols in IElementCodec.ReadCsvAsync(blockNodesFilename, ct))
            {
                var blockNode = BlockNodeDescriptor.Deserialize(cols);
                var h = blockNode.BlockMetadata.Height;

                if (!concBlockToBatch.TryAdd(h, batch))
                {
                    _logger.LogError(
                        "{s} Error on block height {h:n0}; " +
                        "this block is defined at least twice, in batches with names {b1} and {b2}.",
                        _processStep, h, concBlockToBatch[h].Name, batch.Name);
    
                    throw new InvalidDataException();
                }

                concBlockNodes.TryAdd(h, blockNode);
            }

            Interlocked.Increment(ref counter);
            if (counter % 100 == 0)
            {
                _logger.LogInformation(
                    "{s} Finished reading block node files for {n:n0} / {total:n0} batches",
                    _processStep, counter, batches.Count);
        }
        });

        _logger.LogInformation("{s} Finished reading Block node files to create block-to-batch mapping.", _processStep);

        return (new Dictionary<long, Batch>(concBlockToBatch), new SortedDictionary<long, BlockNode>(concBlockNodes));
    }

    private static string GetSpentTxoFilename(Batch batch)
    {
        return Path.Join(
            Path.GetDirectoryName(batch.GetFilename(T2SEdge.Kind)),
            batch.FilenamePrefix + "_spent_utxos.csv");
    }

    private async Task CreatePerBatchSpentUtxo(List<Batch> batches, Dictionary<long, Batch> heightToBatchMapping, CancellationToken ct)
    {
        _logger.LogInformation("{s} Creating per-batch spent Txo files; these are temporary files and can be deleted after process ends.", _processStep);

        var blockToWriterMapping = new Dictionary<string, StreamWriter>();
        foreach (var batch in batches)
            blockToWriterMapping.Add(batch.Name, new StreamWriter(GetSpentTxoFilename(batch)));

        var creationHeightParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.CreationHeight);
        var spentHeightParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.SpentHeight);
        var txidParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.Txid);
        var voutParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.Vout);

        int counter = 0;
        try
        {
            foreach (var batch in batches)
            {
                ct.ThrowIfCancellationRequested();

                await foreach (var cols in IElementCodec.ReadCsvAsync(batch.GetFilename(S2TEdge.Kind), ct))
                {
                    var writer = blockToWriterMapping[heightToBatchMapping[creationHeightParser(cols)].Name];
                    writer.WriteLine(string.Join(Options.CsvDelimiter,
                        txidParser(cols), // preout txid
                        voutParser(cols),  // preout vout
                        spentHeightParser(cols)));
                }

                counter++;
                if (counter % 10 == 0)
                    _logger.LogInformation("{s} Finished creating per-batch spent Txo files for {n} batches.", _processStep, counter);
            }
        }
        finally
        {
            foreach (var writer in blockToWriterMapping.Values)
                writer.Dispose();
        }

        _logger.LogInformation("{s} Finished creating per-batch spent Txo files.", _processStep);
    }

    private async Task SetTxoSpentHeight(List<Batch> batches, CancellationToken ct)
    {
        _logger.LogInformation("{s} Setting Txo spent height for all T2S edges.", _processStep);

        var sourceIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(MappingBuilder.StartIdPropertyName);
        var voutIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.Vout);
        var spentHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.SpentHeight);

        int counter = 0;
        foreach (var batch in batches)
        {
            var spentTxo = new Dictionary<string, long>();
            using (var reader = new StreamReader(GetSpentTxoFilename(batch)))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(Options.CsvDelimiter);
                    var preoutTxid = parts[0];
                    var preoutVout = parts[1];
                    var spentHeight = long.Parse(parts[2]);
                    spentTxo.Add($"{preoutTxid}-{preoutVout}", spentHeight);
                }
            }

            ct.ThrowIfCancellationRequested();

            using var writer = new StreamWriter(new GZipStream(
                File.Create(Helpers.AddPostfixToFilename(batch.GetFilename(T2SEdge.Kind), "_with_txo_spent_height_set")),
                CompressionLevel.Optimal));

            try
            {
                using var reader = new StreamReader(new GZipStream(
                    File.OpenRead(batch.GetFilename(T2SEdge.Kind)),
                    CompressionMode.Decompress));

                string? line;

                while ((line = await reader.ReadLineAsync(ct)) != null)
                {
                    var cols = line.Split(Options.CsvDelimiter);
                    if (spentTxo.TryGetValue($"{cols[sourceIdx]}-{cols[voutIdx]}", out var spentHeight))
                        cols[spentHeightIdx] = spentHeight.ToString();

                    writer.WriteLine(string.Join(Options.CsvDelimiter, cols));
                }
            }
            finally
            {
                writer.Close();
            }

            ct.ThrowIfCancellationRequested();

            counter++;
            if (counter % 10 == 0)
                _logger.LogInformation("{s} Finished setting Txo spent height for {n} batches.", _processStep, counter);
        }

        _logger.LogInformation("{s} Finished setting Txo spent height for all T2S edges.", _processStep);
    }


    private async Task SetSupplyAmount(List<Batch> batches, SortedDictionary<long, BlockNode> blocks, CancellationToken ct)
    {
        if (blocks.First().Value.BlockMetadata.Height != 0)
            throw new InvalidOperationException(
                $"The first block in the graph has height " +
                $"{blocks.First().Value.BlockMetadata.Height:,}, " +
                $"expected 0.");

        if (blocks.Last().Value.BlockMetadata.Height != blocks.Count - 1)
            throw new InvalidOperationException(
                $"This operation requires a continues set of blocks, there are missing blocks");

        blocks[0].BlockMetadata.TotalSupply = blocks[0].BlockMetadata.MintedBitcoins;
        blocks[0].BlockMetadata.TotalSupplyNominal = blocks[0].BlockMetadata.TotalSupply;
        for (var i = 1; i < blocks.Count; i++)
        {
            blocks[i].BlockMetadata.TotalSupply =
                blocks[i - 1].BlockMetadata.TotalSupply +
                blocks[i].TripletTypeValueSum[C2TEdge.Kind] -
                blocks[i].BlockMetadata.ProvablyUnspendableBitcoins;

            blocks[i].BlockMetadata.TotalSupplyNominal =
                blocks[i - 1].BlockMetadata.TotalSupplyNominal + blocks[i].BlockMetadata.MintedBitcoins;
        }

        ct.ThrowIfCancellationRequested();

        var hIndex = BlockNodeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.BlockMetadata.Height);
        var totalSupplyIndex = BlockNodeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.BlockMetadata.TotalSupply);
        var totalSupplyNominalIndex = BlockNodeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.BlockMetadata.TotalSupplyNominal);

        _logger.LogInformation("{s} Updating block node files with total supply information.", _processStep);
        int counter = 0;
        foreach (var batch in batches)
        {
            var blockNodesFilename = batch.GetFilename(BlockNode.Kind);
            using var writer = new StreamWriter(new GZipStream(
                File.Create(Helpers.AddPostfixToFilename(blockNodesFilename, "_supply_updated")),
                CompressionLevel.Optimal));

            try
            {
                await foreach (var cols in IElementCodec.ReadCsvAsync(blockNodesFilename, ct))
                {
                    var h = long.Parse(cols[hIndex]);
                    var b = blocks[h];
                    cols[totalSupplyIndex] = b.BlockMetadata.TotalSupply?.ToString() ?? "0";
                    cols[totalSupplyNominalIndex] = b.BlockMetadata.TotalSupplyNominal?.ToString() ?? "0";

                    writer.WriteLine(string.Join(Options.CsvDelimiter, cols));
                }
            }
            finally
            {
                writer.Close();

                counter++;
                if (counter % 10 == 0)
                    _logger.LogInformation(
                        "{s} Finished updating block node files with total supply information for {n}/{total} batches.", 
                        _processStep, counter, batches.Count);
            }
        }

        _logger.LogInformation(
            "{s} Finished updating block node files with total supply information for {n}/{total} batches.",
            _processStep, counter, batches.Count);
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

    private async Task SetRealizedCap(
        List<Batch> batches,
        SortedDictionary<long, BlockNode> blockNodes, 
        Dictionary<long, OHLCV> ohlcv,
        CancellationToken ct)
    {
        var creationHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.CreationHeight);
        var spentHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.SpentHeight);
        var valueIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.Value);

        var sortedHeights = blockNodes.Keys.ToArray();

        var lockObject = new object();

        await Parallel.ForEachAsync(batches, ct, async (batch, ct) =>
        {
            using var reader = new StreamReader(new GZipStream(
            File.OpenRead(batch.GetFilename(T2SEdge.Kind)),
            CompressionMode.Decompress));

            string? line;

            var processedEdgeCount = 0;
            var skippedEdgeCounter = 0;

            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(Options.CsvDelimiter);

                var creationHeight = long.Parse(cols[creationHeightIdx]);
                var spentHeight = long.Parse(cols[spentHeightIdx]);
                var value = long.Parse(cols[valueIdx]);

                if (!ohlcv.TryGetValue(creationHeight, out var creationBlockOHLCV))
                {
                    skippedEdgeCounter++;
                }
                else
                {
                    var utxoFiatValueAtCreation = creationBlockOHLCV.GetFiatValue(value);
                    if (utxoFiatValueAtCreation > 0)
                    {
                        lock (lockObject)
                        {
                            foreach (var h in sortedHeights.GetViewBetween(creationHeight, spentHeight))
                            {
                                var block = blockNodes[h];
                                block.BlockMetadata.RealizedCap ??= 0;
                                block.BlockMetadata.RealizedCap += utxoFiatValueAtCreation;

                                if (block.BlockMetadata.Ohlcv != null)
                                {
                                    if (utxoFiatValueAtCreation < block.BlockMetadata.Ohlcv.VWAP)
                                    {
                                        block.BlockMetadata.UnrealizedLoss ??= 0;
                                        block.BlockMetadata.UnrealizedLoss += block.BlockMetadata.Ohlcv.VWAP - utxoFiatValueAtCreation;
                                    }
                                    else
                                    {
                                        block.BlockMetadata.UnrealizedProfit ??= 0;
                                        block.BlockMetadata.UnrealizedProfit += utxoFiatValueAtCreation - block.BlockMetadata.Ohlcv.VWAP;
                                    }
                                }
                            }
                        }
                    }

                    processedEdgeCount++;
                }
            }
        });
    }
}
