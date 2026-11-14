using AAB.EBA.Tests.TestKit;
using AAB.EBA.Tests.TestKit.Traverse;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Point = AAB.EBA.Tests.TestKit.Traverse.TraverseCancellationPoint;

namespace AAB.EBA.Tests.Blockchains.Bitcoin;

/// <summary>
/// Resuming a canceled <c>BitcoinOrchestrator.TraverseAsync</c>: a new traverse
/// (new host) in the working directory of the canceled one.
/// </summary>
[Trait("Category", "Functional")]
public class BitcoinOrchestratorTraverseResumeTests : TestsBase, IClassFixture<BaselineTraverseFixture>
{
    private const long From = TraverseOptionsBuilder.DefaultFromHeight;
    private readonly BaselineTraverseFixture _baseline;

    public BitcoinOrchestratorTraverseResumeTests(BaselineTraverseFixture baseline)
    {
        _baseline = baseline;
    }

    [Theory]
    [InlineData(Point.AfterQueuesPersisted)]
    [InlineData(Point.WhileBufferIsFull)]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    [InlineData(Point.WhileWritingBlockWithBufferedBlocks)]
    public async Task TraverseAsync_ResumedAfterCancellation_CompletesTheTraverse(Point point)
    {
        // Arrange
        var canceled = await RunCanceledAsync(point, TempExeDir);
        var workingDir = new TraverseWorkingDir(canceled.Options);
        var before = workingDir.Snapshot();
        var batchFiles = await workingDir.BatchFilesAsync();

        // Act
        await UncanceledTraverse.RunAsync(canceled.Options);
        var actual = await TraverseOutput.ReadAsync(canceled.Options);

        // Assert
        Assert.Multiple(
            () => TraverseAssert.ResumedOutputIsComplete(canceled.RequestedHeights, actual, _baseline.Output),
            () => Assert.Subset(
                workingDir.Snapshot().ToHashSet(),
                before.FilteredTo(batchFiles).ToHashSet()));
    }

    [Fact]
    public async Task TraverseAsync_ResumedAfterTwoCancellations_OutputEqualsUncanceledTraverse()
    {
        // Arrange
        var dir = Path.Join(TempExeDir, "canceled");
        await RunCanceledAsync(Point.WhileWritingBlock, dir, From);
        var canceled = await RunCanceledAsync(Point.WhileWritingBlock, dir, From + 2);

        // Act
        await UncanceledTraverse.RunAsync(canceled.Options);

        // Assert
        TraverseAssert.OutputsEqual(_baseline.Output, await TraverseOutput.ReadAsync(canceled.Options));
    }

    [Fact]
    public async Task TraverseAsync_ResumedWithNothingPending_LeavesOutputUnchanged()
    {
        // Arrange
        var options = await UncanceledTraverse.RunDefaultAsync(TempExeDir);
        var workingDir = new TraverseWorkingDir(options);
        var before = workingDir.Snapshot();
        var batchFiles = await workingDir.BatchFilesAsync();

        // Act
        await UncanceledTraverse.RunAsync(options);

        // Assert
        Assert.Equal(
            before.FilteredTo(batchFiles),
            workingDir.Snapshot().FilteredTo(batchFiles));
        Assert.Equal(
            options.Bitcoin.Traverse.To - options.Bitcoin.Traverse.From + 1,
            (await TraverseOutput.ReadAsync(options)).WrittenBlockHeights.Count);
    }

    private static async Task<CanceledTraverse> RunCanceledAsync(
        Point point, string workingDir, long? height = null)
    {
        using var scenario = TraverseCancellationScenario.Create(
            point,
            TraverseOptionsBuilder.ForWorkingDir(workingDir),
            height ?? TraverseCancellationScenario.GetDefaultHeight(point));
        await scenario.RunUntilCanceledAsync();
        return new CanceledTraverse(scenario.Options, scenario.RequestedHeights);
    }

    private sealed record CanceledTraverse(
        AAB.EBA.CLI.Config.Options Options,
        System.Collections.Generic.IReadOnlyList<long> RequestedHeights);
}
