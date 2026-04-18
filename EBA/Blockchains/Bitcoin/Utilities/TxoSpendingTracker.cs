using EBA.Graph.Bitcoin.Descriptors;
using EBA.Graph.Db.Neo4jDb;
using System.IO.Compression;

namespace EBA.Blockchains.Bitcoin.Utilities;

public class TxoSpendingTracker
{
    public async Task UpdatePostTraverse(Options options)
    {
        var batches = await Batch.DeserializeBatchesAsync(options.Bitcoin.MapSpends.BatchesFilename);

        var blockHeightToBatchMapping = await GetBlockHeightToBatchMapping(options, batches);
        await CreatePerBatchSpentTxo(batches, blockHeightToBatchMapping);
        await SetTxoSpentHeight(batches);
    }

    private static async Task<Dictionary<long, Batch>> GetBlockHeightToBatchMapping(Options options, List<Batch> batches)
    {
        var heightParser = BlockNodeDescriptor.StaticMapper.GetFieldParser(x => x.BlockMetadata.Height);
        var mapping = new Dictionary<long, Batch>();

        //var codec = new CodecBase<BlockNode>(null, null, null);

        foreach (var batch in batches)
        {
            var blockNodesFilename = batch.GetFilename(BlockNode.Kind);

            await foreach (var cols in IElementCodec.ReadCsvAsync(blockNodesFilename))
                mapping.Add(heightParser(cols), batch);
        }

        return mapping;
    }

    private static string GetSpentTxoFilename(Batch batch)
    {
        return Path.Join(
            Path.GetDirectoryName(batch.GetFilename(T2SEdge.Kind)),
            batch.FilenamePrefix + "_spent_utxo.tsv");
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
                writer.WriteLine(string.Join('\t',
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
        foreach (var batch in batches)
        {
            var spentTxo = new Dictionary<string, long>();
            using (var reader = new StreamReader(GetSpentTxoFilename(batch)))
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

            var writer = new StreamWriter(Path.Join(Path.GetDirectoryName(batch.GetFilename(T2SEdge.Kind)), batch.FilenamePrefix + "_Tx_Credits_Script.csv"));
            using (
                Stream stream = File.OpenRead(createdTxes),
                zippedStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                using StreamReader reader = new(zippedStream);
                var line = "";

                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split('\t');
                    var txid = parts[0];
                    var vout = int.Parse(parts[3]);

                    if (spentTxo.TryGetValue($"{txid}-{vout}", out var spentHeight))
                    {
                        writer.WriteLine(string.Join('\t',
                            txid, // source
                            parts[1], // target
                            parts[2], // value
                            vout,
                            parts[4], // creation height
                            spentHeight,
                            parts[6])); // type label
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            writer.Close();
        }
    }
}
