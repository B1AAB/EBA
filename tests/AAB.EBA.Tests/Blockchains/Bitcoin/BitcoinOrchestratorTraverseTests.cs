using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Graph;
using AAB.EBA.Tests.TestKit.Traverse;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace AAB.EBA.Tests.Blockchains.Bitcoin;

[Trait("Category", "Unit")]
public class BitcoinOrchestratorTraverseTests : TestsBase
{
    [Theory]
    [InlineData(71036)]
    [InlineData(132317)]
    [InlineData(717531)]
    [InlineData(71037)]
    public async Task TraverseAsync_SingleBlock_WritesExpectedGraph(int height)
    {
        // Arrange
        var options = await RecordedTraverse.RunAsync(TempExeDir, height, height);

        // Act
        var output = await TraverseGraphOutput.ReadAsync(options);

        // Assert
        ExpectedGraphStore.Compare(height, output, BitcoinGraphColumns.Stable);
    }

    [Fact]
    public async Task TraverseAsync_BlockRange_WritesGraphOfEveryBlock()
    {
        // Arrange
        var options = await RecordedTraverse.RunAsync(TempExeDir, 71036, 71037);

        // Act
        var output = await TraverseGraphOutput.ReadAsync(options);

        // Assert
        GraphCsvAssert.Equal(ExpectedGraphStore.Union(71036, 71037).Tables, output.Tables, BitcoinGraphColumns.Stable);
    }

    [Fact]
    public async Task TraverseAsync_Completed_LeavesNoBlockPending()
    {
        // Arrange
        var options = await RecordedTraverse.RunAsync(TempExeDir, 71036, 71037);

        // Act
        var output = await TraverseOutput.ReadAsync(options);

        // Assert
        TraverseAssert.EveryBlockAccountedExactlyOnce([71036, 71037], output);
        Assert.Empty(output.PendingBlockHeights);
        Assert.Empty(output.ToProcessBlockHeights);
        Assert.Empty(output.FailedBlockHeights);
        Assert.Equal([71036L, 71037L], output.WrittenBlockHeights.Order());
    }

    [Fact]
    public async Task TraverseAsync_BlockRange_RequestsEachBlockOnce()
    {
        // Arrange
        using var node = RecordedBitcoinNode.FromMockData();
        var options = TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithRange(71036, 71037).Build();

        // Act
        await new TraverseRunner(options, node).RunAsync(CancellationToken.None);

        // Assert
        Assert.NotEmpty(node.Requests.Where(x => x.Endpoint == BitcoinCoreEndpoint.ChainInfo));
        Assert.Equal([71036L, 71037L], node.Requests.Where(x => x.Endpoint == BitcoinCoreEndpoint.BlockHashByHeight).Select(x => x.Height!.Value).Order());
        Assert.Equal([71036L, 71037L], node.Requests.Where(x => x.Endpoint == BitcoinCoreEndpoint.Block).Select(x => x.Height!.Value).Order());
        Assert.Empty(node.UnexpectedRequests);
    }

    [Theory]
    [InlineData(71036)]
    [InlineData(132317)]
    [InlineData(717531)]
    public async Task TraverseAsync_SingleBlock_WritesExpectedScriptTypeProfile(int height)
    {
        // Arrange
        var options = await RecordedTraverse.RunAsync(TempExeDir, height, height);

        // Act
        var output = await TraverseGraphOutput.ReadAsync(options);

        // Assert
        ExpectedGraphStore.Compare(height, output,
            new GraphCsvComparison().OnlyColumns(BitcoinGraphColumns.ScriptTypeProfile), "nodes_Block");
    }

    [Theory]
    [InlineData(71036)]
    [InlineData(132317)]
    [InlineData(717531)]
    public async Task TraverseAsync_SingleBlock_WritesFeaturesOfEveryTransaction(int height)
    {
        // Arrange
        var options = await RecordedTraverse.RunAsync(TempExeDir, height, height);

        // Act
        var output = await TraverseGraphOutput.ReadAsync(options);

        // Assert
        ExpectedGraphStore.Compare(height, output, onlyType: "nodes_Tx");
    }
}
