using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;

namespace AAB.EBA.Blockchains.Bitcoin.Utilities;

public static class BitcoinHelpers
{
    public static async Task<(Dictionary<long, Batch>, SortedDictionary<long, BlockNode>)> GetHeightToBatchMapping(
        List<Batch> batches,
        ILogger<BitcoinOrchestrator> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Reading Block node files to create block-to-batch mapping.");

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
                    logger.LogError(
                        "Error on block height {h:n0}; " +
                        "this block is defined at least twice, in batches with names {b1} and {b2}.",
                        h, concBlockToBatch[h].Name, batch.Name);

                    throw new InvalidDataException();
                }

                concBlockNodes.TryAdd(h, blockNode);
            }

            Interlocked.Increment(ref counter);
            if (counter % 100 == 0)
            {
                logger.LogInformation(
                    "Finished reading block node files for {n:n0} / {total:n0} batches",
                    counter, batches.Count);
            }
        });

        logger.LogInformation("Finished reading Block node files to create block-to-batch mapping.");

        return (new Dictionary<long, Batch>(concBlockToBatch), new SortedDictionary<long, BlockNode>(concBlockNodes));
    }
}
