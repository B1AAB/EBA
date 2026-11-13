using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests.Graph.Db.Neo4jDb;

[Trait("Category", "Unit")]
public class BatchTests : TestsBase
{
    [Fact]
    public void Constructor_TwoBatchesCreatedBackToBack_ShareNoFilename()
    {
        // Arrange
        using var strategies = new BitcoinStrategyFactory(new Options(TempExeDir));

        // Act
        var first = new Batch("0", TempExeDir, strategies.NodeStrategies, strategies.EdgeStrategies);
        var second = new Batch("1", TempExeDir, strategies.NodeStrategies, strategies.EdgeStrategies, previousBatch: first);

        // Assert
        Assert.Empty(first.TypesInfo.Values.Select(x => x.Filename)
            .Intersect(second.TypesInfo.Values.Select(x => x.Filename)));
    }

    [Fact]
    public void Constructor_NewBatch_FilenamesUseTimestampOnlyPrefix()
    {
        // Arrange
        using var strategies = new BitcoinStrategyFactory(new Options(TempExeDir));

        // Act
        var batch = new Batch("0", TempExeDir, strategies.NodeStrategies, strategies.EdgeStrategies);

        // Assert
        Assert.Matches("^[0-9]+$", batch.FilenamePrefix);
        Assert.All(batch.TypesInfo.Values, x => Assert.Matches("^[0-9]+_(nodes|edges)_", Path.GetFileName(x.Filename)));
    }

    [Fact]
    public void Constructor_PreviousBatchPrefixAheadOfClock_AdvancesPrefix()
    {
        // Arrange
        using var strategies = new BitcoinStrategyFactory(new Options(TempExeDir));
        var previous = StoredBatch("0", "9999999999", Path.Join(TempExeDir, "9999999999_nodes_Block.csv.gz"));

        // Act
        var batch = new Batch("1", TempExeDir, strategies.NodeStrategies, strategies.EdgeStrategies, previousBatch: previous);

        // Assert
        Assert.Equal("10000000000", batch.FilenamePrefix);
        Assert.All(batch.TypesInfo.Values, x => Assert.StartsWith("10000000000_", Path.GetFileName(x.Filename)));
    }

    [Theory]
    [InlineData("1794456334")]
    [InlineData("1794456334_0")]
    public async Task DeserializeBatchesAsync_ManifestWithStoredPrefix_KeepsStoredFilenames(string prefix)
    {
        // Arrange
        var manifest = Path.Join(TempExeDir, "batches.json");
        var filename = Path.Join(TempExeDir, $"{prefix}_nodes_Block.csv.gz");
        var batch = StoredBatch("0", prefix, filename);
        await Batch.SerializeBatchesAsync(manifest, [batch]);

        // Act
        var restored = Assert.Single(await Batch.DeserializeBatchesAsync(manifest));

        // Assert
        Assert.Equal(batch.Name, restored.Name);
        Assert.Equal(batch.FilenamePrefix, restored.FilenamePrefix);
        Assert.Equal(filename, restored.TypesInfo["Block"].Filename);
    }

    [Fact]
    public async Task DeserializeBatchesAsync_TwoBatchesListingOneFile_ThrowsInvalidDataException()
    {
        // Arrange
        var manifest = Path.Join(TempExeDir, "batches.json");
        var filename = Path.Join(TempExeDir, "1794456334_nodes_Block.csv.gz");
        await Batch.SerializeBatchesAsync(manifest,
            [StoredBatch("1332", "1794456334", filename), StoredBatch("1333", "1794456334", filename)]);

        // Act
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => Batch.DeserializeBatchesAsync(manifest));

        // Assert
        Assert.Contains("1332", error.Message);
        Assert.Contains("1333", error.Message);
        Assert.Contains(filename, error.Message);
        Assert.Contains("same-second", error.Message);
    }

    [Fact]
    public async Task DeserializeBatchesAsync_TwoBatchesWithOneName_ThrowsInvalidDataException()
    {
        // Arrange
        var manifest = Path.Join(TempExeDir, "batches.json");
        await Batch.SerializeBatchesAsync(manifest,
            [StoredBatch("1332", "1794456334", "first.csv.gz"), StoredBatch("1332", "1794456335", "second.csv.gz")]);

        // Act
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => Batch.DeserializeBatchesAsync(manifest));

        // Assert
        Assert.Contains("1332", error.Message);
    }

    private Batch StoredBatch(string name, string prefix, string filename)
    {
        return new Batch(name, TempExeDir, prefix, new Dictionary<string, TypeInfo>
        {
            ["Block"] = new TypeInfo(filename, 1)
        });
    }
}
