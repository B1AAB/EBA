using EBA.Graph.Bitcoin.Descriptors;
using EBA.Graph.Db.Neo4jDb;
using EBA.Utilities;
using System.IO.Compression;

namespace EBA.Blockchains.Bitcoin.Utilities;

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
        _logger.LogInformation("Deserialized {n} batches from {filename}.", batches.Count, _options.Bitcoin.MapSpends.BatchesFilename);

        _processStep = "[1/4] Block-to-batch mapping:";
        GetHeightToBatchMapping(_options, batches, out var blockToBatch, out var blockNodes);

        _processStep = "[2/4] Collecting Txo spending:";
        await CreatePerBatchSpentUtxo(batches, blockToBatch, ct);

        _processStep = "[3/4] Setting UTxO spending:";
        await SetTxoSpentHeight(batches, ct);

        _processStep = "[4/4] Setting supply amount:";
        await SetSupplyAmount(batches, blockNodes, ct);
    }

    private void GetHeightToBatchMapping(
        Options options,
        List<Batch> batches,
        out Dictionary<long, Batch> blockToBatch,
        out SortedDictionary<long, BlockNode> blockNodes)
    {
        _logger.LogInformation("{s} Reading Block node files to create block-to-batch mapping.", _processStep);

        blockToBatch = [];
        blockNodes = [];

        int counter = 0;

        foreach (var batch in batches)
        {
            var blockNodesFilename = batch.GetFilename(BlockNode.Kind);

            foreach (var cols in IElementCodec.ReadCsv(blockNodesFilename))
            {
                var blockNode = BlockNodeDescriptor.Deserialize(cols);
                var h = blockNode.BlockMetadata.Height;
                blockToBatch.Add(h, batch);
                blockNodes[h] = blockNode;
            }

            counter++;
            if (counter % 100 == 0)
                _logger.LogInformation("{s} Finished reading block node files for {n} batches", _processStep, counter);
        }

        _logger.LogInformation("{s} Finished reading Block node files to create block-to-batch mapping.", _processStep);
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
}
