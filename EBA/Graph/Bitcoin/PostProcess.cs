using EBA.Graph.Bitcoin.Factories;
using EBA.Graph.Bitcoin.Strategies;
using EBA.Graph.Db.Neo4jDb;
using System.IO.Compression;

namespace EBA.Graph.Bitcoin;

public class PostProcess
{
    private readonly Options _options;
    private readonly IGraphDb<BitcoinGraph> _graphDb;
    private readonly ILogger<BitcoinGraphAgent> _logger;

    public PostProcess(Options options, IGraphDb<BitcoinGraph> graphDb, ILogger<BitcoinGraphAgent> logger)
    {
        _options = options;
        _graphDb = graphDb;
        _logger = logger;
    }

    private async Task<Dictionary<long, Batch>> GetBlockHeightToBatchMapping(List<Batch> batches)
    {
        var blockStrategy = new BlockNodeStrategy(true);

        var mapping = new Dictionary<long, Batch>();

        foreach (var batch in batches)
        {
            var blockNodesFilename = batch.GetFilename(NodeKind.Block);

            // todo: is there a base type somewhere to read csv?!
            using Stream stream = File.OpenRead(blockNodesFilename), zippedStream = new GZipStream(stream, CompressionMode.Decompress);
            using StreamReader reader = new(zippedStream);
            var line = "";
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split('\t');
                var blockNode = BlockNodeStrategy.Deserialize(parts);
                mapping.Add(blockNode.BlockMetadata.Height, batch);
            }
        }

        return mapping;
    }

    private async Task CreatePerBatchSpentTxo(List<Batch> batches, Dictionary<long, Batch> blockHeightToBatchMapping)
    {
        // post-process-graph
        
        var blockToWriterMapping = new Dictionary<string, StreamWriter>();
        foreach (var batch in batches)
        {
            blockToWriterMapping.Add(batch.Name, new StreamWriter(
                Path.Join(
                    Path.GetDirectoryName(batch.GetFilename(T2SEdge.Kind)),
                    batch.FilenamePrefix + "_spent_utxo.tsv")));
        }


        foreach (var batch in batches)
        {
            var filename = batch.GetFilename(S2TEdge.Kind);

            using (
                Stream fileStream = File.OpenRead(filename), 
                zippedStream = new GZipStream(fileStream, CompressionMode.Decompress))
            {
                using (StreamReader reader = new(zippedStream))
                {
                    var line = "";

                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split('\t');
                        var height = long.Parse(parts[3]);
                        var preoutHeight = long.Parse(parts[7]);
                        var preoutTxid = parts[4];
                        var preoutVout = int.Parse(parts[5]);

                        var prevoutBatch = blockHeightToBatchMapping[preoutHeight];
                        var writer = blockToWriterMapping[prevoutBatch.Name];

                        writer.WriteLine($"{preoutTxid}\t{preoutVout}\t{height}");
                    }
                }
            }
        }

        foreach (var writer in blockToWriterMapping.Values)
        {
            writer.Dispose();
        }
    }

    private async Task SetTxoSpentHeight(List<Batch> batches)
    {
        foreach (var batch in batches)
        {
            var spentTxo = new Dictionary<string, long>();
            using (var reader = new StreamReader(
                Path.Join(
                    Path.GetDirectoryName(batch.GetFilename(T2SEdge.Kind)),
                    batch.FilenamePrefix + "_spent_utxo.tsv")))
            {
                var line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split('\t');
                    var preoutTxid = parts[0];
                    var preoutVout = parts[1];
                    var spentHeight = long.Parse(parts[2]);
                    spentTxo.Add($"{preoutTxid}-{preoutVout}", spentHeight);
                }
            }

            var createdTxes = batch.GetFilename(T2SEdge.Kind);

            var writer = new StreamWriter(Path.Join(Path.GetDirectoryName(batch.GetFilename(T2SEdge.Kind)), batch.FilenamePrefix + "_created_txo.tsv"));
            using (
                Stream stream = File.OpenRead(createdTxes),
                zippedStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                using (StreamReader reader = new(zippedStream))
                {
                    var line = "";

                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split('\t');
                        var txid = parts[0];
                        var target = parts[1];
                        var value = parts[2];
                        var vout = int.Parse(parts[3]);
                        var creationHeight = parts[4];
                        var spentHeight = parts[5];
                        var typeLabel = parts[6];

                        if (spentTxo.TryGetValue($"{txid}-{vout}", out var spentHeightBB))
                        {
                            writer.WriteLine($"{txid}\t{vout}\t{value}\t{creationHeight}\t{spentHeightBB}\t{typeLabel}");
                        }
                        else
                        {
                            writer.WriteLine(line);
                        }
                    }
                }
            }

            writer.Close();
        }
    }

    public async Task Run()
    {
        var batches = await Batch.DeserializeBatchesAsync(
            @"D:\eba_spent_utxo\EBA\bin\Debug\net10.0\session_1775872243\batches.json");// _options.Neo4j.BatchesFilename);

        var blockHeightToBatchMapping = await GetBlockHeightToBatchMapping(batches);
        await CreatePerBatchSpentTxo(batches, blockHeightToBatchMapping);
        await SetTxoSpentHeight(batches);

        return;

        var blocks = new SortedList<long, BlockNode>();
        var nodeVar = "n";

        _logger.LogInformation("Fetching blocks from graph database.");
        var records = await _graphDb.GetNodesAsync(NodeKind.Block, CancellationToken.None, nodeVariable: nodeVar);
        _logger.LogInformation("Retrieved {count:n0} records from graph database. Creating block nodes.", records.Count);

        foreach (var record in records)
        {
            NodeFactory.TryCreate(record[nodeVar].As<Neo4j.Driver.INode>(), out var blockNode);
            var block = (BlockNode)blockNode;
            blocks.Add(block.BlockMetadata.Height, block);
        }

        if (blocks.First().Value.BlockMetadata.Height != 0)
        {
            throw new InvalidOperationException(
                $"The first block in the graph has height " +
                $"{blocks.First().Value.BlockMetadata.Height:,}, " +
                $"expected 0.");
        }

        if (blocks.Last().Value.BlockMetadata.Height != blocks.Count - 1)
        {
            throw new InvalidOperationException(
                $"This operation requires a continues set of blocks, there are missing blocks");
        }

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

        var updates = blocks.Values.Select(b => new Dictionary<string, object?>
        {
            [nameof(BlockMetadata.Height)] = b.BlockMetadata.Height,
            [nameof(BlockMetadata.TotalSupply)] = b.BlockMetadata.TotalSupply,
            [nameof(BlockMetadata.TotalSupplyNominal)] = b.BlockMetadata.TotalSupplyNominal,
        }).ToList();

        _logger.LogInformation("Pushing {count:n0} block updates to graph database.", updates.Count);
        await _graphDb.BulkUpdateNodePropertiesAsync(
            NodeKind.Block,
            nameof(BlockMetadata.Height),
            updates,
            CancellationToken.None);
        _logger.LogInformation("Completed pushing block updates.");
    }
}
