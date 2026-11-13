using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Graph.Model;
using AAB.EBA.Tests.TestKit;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Traverse;

using Microsoft.Extensions.Logging.Abstractions;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests.Graph.Db.Neo4jDb;

/// <summary>
/// "Earlier run" = another instance serialized a graph to the same working
/// directory and was disposed, as a previous (e.g., canceled) traverse does.
/// </summary>
[Trait("Category", "Unit")]
public class BitcoinNeo4jDbBatchingTests : TestsBase
{
    private const long First = TraverseOptionsBuilder.DefaultFromHeight;
    private const long Second = First + 1;

    [Fact]
    public async Task SerializeAsync_WhenBatchFills_WritesNextGraphToDifferentFiles()
    {
        // Arrange
        var options = TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithMaxEntitiesPerBatch(1).Build();

        // Act
        await SerializeFiveGraphsAsync(options);

        // Assert
        var batches = await new TraverseWorkingDir(options).BatchesAsync();
        Assert.Equal(5, batches.Count);
        var filenames = batches.SelectMany(x => x.TypesInfo.Values).Select(x => x.Filename).ToList();
        Assert.Equal(filenames.Count, filenames.Distinct().Count());
    }

    [Fact]
    public async Task SerializeAsync_WhenBatchFills_WritesEachGraphToItsOwnBatch()
    {
        // Arrange
        var options = TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithMaxEntitiesPerBatch(1).Build();

        // Act
        await SerializeFiveGraphsAsync(options);

        // Assert
        var batches = await new TraverseWorkingDir(options).BatchesAsync();
        Assert.Equal(5, batches.Count);
        for (var i = 0; i < batches.Count; i++)
        {
            var row = Assert.Single(IElementCodec.ReadCsv(batches[i].GetFilename(BlockNode.Kind)));
            Assert.Equal(First + i, BlockNodeDescriptor.Deserialize(row).BlockMetadata.Height);
        }
    }

    [Fact]
    public async Task SerializeAsync_OverEarlierBatches_StartsNewBatch()
    {
        // Arrange
        var options = CreateOptions();
        await SerializeEarlierRunAsync(options);
        var graph = await SyntheticBlockGraph.CreateAsync(Second, options);
        using var db = CreateDb(options);

        // Act
        await db.SerializeAsync(graph, CancellationToken.None);

        // Assert
        Assert.Equal(2, (await new TraverseWorkingDir(options).BatchesAsync()).Count);
    }

    [Fact]
    public async Task SerializeAsync_OverEarlierBatches_LeavesEarlierBatchFilesUnchanged()
    {
        // Arrange
        var options = CreateOptions();
        await SerializeEarlierRunAsync(options);
        var before = await TraverseOutput.ReadAsync(options);
        var graph = await SyntheticBlockGraph.CreateAsync(Second, options);
        var db = CreateDb(options);

        // Act
        await db.SerializeAsync(graph, CancellationToken.None);
        db.Dispose();

        // Assert
        var after = await TraverseOutput.ReadAsync(options);
        TraverseAssert.BlocksEqual(before, after, [First]);
    }

    [Fact]
    public async Task SerializeAsync_CalledTwice_WritesBothGraphsToOneBatch()
    {
        // Arrange
        var options = CreateOptions();
        var first = await SyntheticBlockGraph.CreateAsync(First, options);
        var second = await SyntheticBlockGraph.CreateAsync(Second, options);
        var db = CreateDb(options);

        // Act
        await db.SerializeAsync(first, CancellationToken.None);
        await db.SerializeAsync(second, CancellationToken.None);
        db.Dispose();

        // Assert
        Assert.Single(await new TraverseWorkingDir(options).BatchesAsync());
        Assert.Equal([First, Second], (await TraverseOutput.ReadAsync(options)).WrittenBlockHeights);
    }

    [Fact]
    public async Task Dispose_AfterSerialize_LeavesReadableBatchFiles()
    {
        // Arrange
        var options = CreateOptions();
        var graph = await SyntheticBlockGraph.CreateAsync(First, options);
        var db = CreateDb(options);
        await db.SerializeAsync(graph, CancellationToken.None);

        // Act
        db.Dispose();

        // Assert
        Assert.Empty(await new TraverseWorkingDir(options).UnreadableBatchFilesAsync());
    }

    private Options CreateOptions()
    {
        return TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithRange((int)First, (int)Second).Build();
    }

    private static async Task SerializeFiveGraphsAsync(Options options)
    {
        using var db = CreateDb(options);
        for (var height = First; height < First + 5; height++)
            await db.SerializeAsync(await SyntheticBlockGraph.CreateAsync(height, options), CancellationToken.None);
    }

    private static async Task SerializeEarlierRunAsync(Options options)
    {
        var graph = await SyntheticBlockGraph.CreateAsync(First, options);
        using (var db = CreateDb(options))
            await db.SerializeAsync(graph, CancellationToken.None);
    }

    private static BitcoinNeo4jDb CreateDb(Options options)
    {
        return new BitcoinNeo4jDb(options, NullLogger<Neo4jDb<BitcoinGraph>>.Instance);
    }
}
