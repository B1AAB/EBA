using EBA.Graph.Bitcoin.Descriptors;
using EBA.Graph.Db.Neo4jDb;
using EBA.Utilities;
using System.IO.Compression;

namespace EBA.Blockchains.Bitcoin.Utilities;

public class TxoSpendingTracker
{
    private ILogger<BitcoinOrchestrator> _logger;

    public async Task UpdatePostTraverse(ILogger<BitcoinOrchestrator> logger, Options options)
    {
        _logger = logger;        

        var batches = await Batch.DeserializeBatchesAsync(options.Bitcoin.MapSpends.BatchesFilename);
        GetBlockHeightToBatchMapping(options, batches, out var blockToBatch, out var blockNodes);
        await CreatePerBatchSpentTxo(batches, blockToBatch);
        await SetTxoSpentHeight(batches);
        await SetSupplyAmount(batches, blockNodes);
    }

    private void GetBlockHeightToBatchMapping(
        Options options, 
        List<Batch> batches, 
        out Dictionary<long, Batch> blockToBatch,
        out SortedDictionary<long, BlockNode> blockNodes)
    {
        _logger.LogInformation("Reading Block node files to create block-to-batch mapping...");

        blockToBatch = [];
        blockNodes = [];

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
        }
    }

    private static string GetSpentTxoFilename(Batch batch)
    {
        return Path.Join(
            Path.GetDirectoryName(batch.GetFilename(T2SEdge.Kind)),
            batch.FilenamePrefix + "_spent_utxo.csv");
    }

    private static async Task CreatePerBatchSpentTxo(List<Batch> batches, Dictionary<long, Batch> blockHeightToBatchMapping)
    {
        var blockToWriterMapping = new Dictionary<string, StreamWriter>();
        foreach (var batch in batches)
            blockToWriterMapping.Add(batch.Name, new StreamWriter(GetSpentTxoFilename(batch)));

        var creationHeightParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.CreationHeight);
        var spentHeightParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.SpentHeight);
        var txidParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.Txid);
        var voutParser = S2TEdgeDescriptor.StaticMapper.GetFieldParser(x => x.Vout);

        foreach (var batch in batches)
        {
            await foreach (var cols in IElementCodec.ReadCsvAsync(batch.GetFilename(S2TEdge.Kind)))
            {
                var writer = blockToWriterMapping[blockHeightToBatchMapping[creationHeightParser(cols)].Name];
                writer.WriteLine(string.Join(Options.CsvDelimiter,
                    txidParser(cols), // preout txid
                    voutParser(cols),  // preout vout
                    spentHeightParser(cols)));
            }
        }

        foreach (var writer in blockToWriterMapping.Values)
            writer.Dispose();
    }

    private static async Task SetTxoSpentHeight(List<Batch> batches)
    {
        var sourceIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(MappingBuilder.StartIdPropertyName);
        var voutIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.Vout);
        var spentHeightIdx = T2SEdgeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.SpentHeight);

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

            var createdTxes = batch.GetFilename(T2SEdge.Kind);

            using var writer = new StreamWriter(new GZipStream(
                File.Create(Helpers.AddPostfixToFilename(batch.GetFilename(T2SEdge.Kind), "_Tx_Credits_Script")), 
                CompressionLevel.Optimal));

            using (
                Stream stream = File.OpenRead(createdTxes),
                zippedStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                using StreamReader reader = new(zippedStream);
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    var cols = line.Split(Options.CsvDelimiter);
                    if (spentTxo.TryGetValue($"{cols[sourceIdx]}-{cols[voutIdx]}", out var spentHeight))
                        cols[spentHeightIdx] = spentHeight.ToString();
                    
                    writer.WriteLine(string.Join(Options.CsvDelimiter, cols));
                }
            }

            writer.Close();
        }
    }


    private async Task SetSupplyAmount(List<Batch> batches, SortedDictionary<long, BlockNode> blocks)
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

        var hIndex = BlockNodeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.BlockMetadata.Height);
        var totalSupplyIndex = BlockNodeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.BlockMetadata.TotalSupply);
        var totalSupplyNominalIndex = BlockNodeDescriptor.StaticMapper.GetPropertyCsvIndex(x => x.BlockMetadata.TotalSupplyNominal);

        foreach (var batch in batches)
        {
            _logger.LogInformation("Updating block node files with total supply information...");

            var blockNodesFilename = batch.GetFilename(BlockNode.Kind);
            using var writer = new StreamWriter(new GZipStream(
                File.Create(Helpers.AddPostfixToFilename(blockNodesFilename, "_supply_set")), 
                CompressionLevel.Optimal));

            foreach (var cols in IElementCodec.ReadCsv(blockNodesFilename))
            {
                var h = long.Parse(cols[hIndex]);
                var b = blocks[h];
                cols[totalSupplyIndex] = b.BlockMetadata.TotalSupply?.ToString() ?? "0";
                cols[totalSupplyNominalIndex] = b.BlockMetadata.TotalSupplyNominal?.ToString() ?? "0";

                writer.WriteLine(string.Join(Options.CsvDelimiter, cols));
            }
        }

        _logger.LogInformation("Finished updating block node files with total supply information.");
    }
}
