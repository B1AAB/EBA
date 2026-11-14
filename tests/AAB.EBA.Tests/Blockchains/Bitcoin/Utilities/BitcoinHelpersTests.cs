using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Blockchains.Bitcoin.Utilities;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Graph;
using AAB.EBA.Tests.TestKit.Traverse;

using Microsoft.Extensions.Logging.Abstractions;

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests.Blockchains.Bitcoin.Utilities;

[Trait("Category", "Unit")]
public class BitcoinHelpersTests : TestsBase
{
    private const long First = TraverseOptionsBuilder.DefaultFromHeight;

    [Fact]
    public async Task GetHeightToBatchMapping_BatchesFilledWithinOneSecond_MapsEveryHeightToOneBatch()
    {
        // Arrange
        var options = TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithMaxEntitiesPerBatch(1).Build();
        using (var db = TestGraphDb.Create(options))
        {
            await SyntheticBlockGraph.SerializeAsync(db, options, Enumerable.Range((int)First, 5).Select(x => (long)x).ToArray());
        }
        var batches = await Batch.DeserializeBatchesAsync(options.Neo4j.BatchesFilename);

        // Act
        var (mapping, nodes) = await BitcoinHelpers.GetHeightToBatchMapping(
            batches, NullLogger<BitcoinOrchestrator>.Instance, CancellationToken.None);

        // Assert
        Assert.Equal(Enumerable.Range((int)First, 5).Select(x => (long)x), mapping.Keys.Order());
        Assert.Equal(5, nodes.Count);
        for (var i = 0; i < batches.Count; i++)
            Assert.Same(batches[i], mapping[First + i]);
    }

    [Fact]
    public async Task GetHeightToBatchMapping_HeightWrittenToTwoBatches_ThrowsInvalidDataException()
    {
        // Arrange
        var firstOptions = TraverseOptionsBuilder.ForWorkingDir(Path.Join(TempExeDir, "first")).Build();
        var secondOptions = TraverseOptionsBuilder.ForWorkingDir(Path.Join(TempExeDir, "second")).Build();
        using (var db = TestGraphDb.Create(firstOptions))
            await SyntheticBlockGraph.SerializeAsync(db, firstOptions, new[] { First });
        using (var db = TestGraphDb.Create(secondOptions))
            await SyntheticBlockGraph.SerializeAsync(db, secondOptions, new[] { First });
        var first = Assert.Single(await Batch.DeserializeBatchesAsync(firstOptions.Neo4j.BatchesFilename));
        var storedSecond = Assert.Single(await Batch.DeserializeBatchesAsync(secondOptions.Neo4j.BatchesFilename));
        var second = new Batch("1", storedSecond.DefaultDirectory, storedSecond.FilenamePrefix, storedSecond.TypesInfo);

        // Act
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => BitcoinHelpers.GetHeightToBatchMapping(
            [first, second], NullLogger<BitcoinOrchestrator>.Instance, CancellationToken.None));

        // Assert
        Assert.Contains(First.ToString(), error.Message);
        Assert.Matches(@"names (0 and 1|1 and 0)", error.Message);
    }
}
