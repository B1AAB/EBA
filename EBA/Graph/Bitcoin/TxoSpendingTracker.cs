using EBA.Graph.Bitcoin.Strategies;
using EBA.Graph.Db.Neo4jDb;
using System.IO.Compression;

namespace EBA.Graph.Bitcoin;

public class TxoSpendingTracker
{
    public async Task UpdatePostTraverse(Options options)
    {
        var batches = await Batch.DeserializeBatchesAsync(options.Bitcoin.MapSpends.BatchesFilename);

        var blockHeightToBatchMapping = await GetBlockHeightToBatchMapping(batches);
        await CreatePerBatchSpentTxo(batches, blockHeightToBatchMapping);
        await SetTxoSpentHeight(batches);
    }

    private async Task<Dictionary<long, Batch>> GetBlockHeightToBatchMapping(List<Batch> batches)
    {
        var blockStrategy = new BlockNodeStrategy(true);

        var mapping = new Dictionary<long, Batch>();

        foreach (var batch in batches)
        {
            var blockNodesFilename = batch.GetFilename(NodeKind.Block);

            using Stream stream = File.OpenRead(blockNodesFilename), zippedStream = new GZipStream(stream, CompressionMode.Decompress);
            using StreamReader reader = new(zippedStream);
            var line = "";
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split('\t');
                mapping.Add(long.Parse(parts[1]), batch);
            }
        }

        return mapping;
    }

    private async Task CreatePerBatchSpentTxo(List<Batch> batches, Dictionary<long, Batch> blockHeightToBatchMapping)
    {
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
                            writer.WriteLine($"{txid}\t{target}\t{value}\t{vout}\t{creationHeight}\t{spentHeightBB}\t{typeLabel}");
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
}
