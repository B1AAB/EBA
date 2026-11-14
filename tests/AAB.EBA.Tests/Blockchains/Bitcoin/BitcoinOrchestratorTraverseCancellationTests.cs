using AAB.EBA.Tests.TestKit;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Traverse;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Point = AAB.EBA.Tests.TestKit.Traverse.TraverseCancellationPoint;

namespace AAB.EBA.Tests.Blockchains.Bitcoin;

/// <summary>
/// Cancellation of <c>BitcoinOrchestrator.TraverseAsync</c> at every point of
/// its progress (see <see cref="TraverseCancellationPoint"/>); 6 blocks
/// 71036..71041 unless noted.
///
/// Not testable from the test project:
/// - The window between taking a block from the buffer and marking the
///   listener as busy (PersistentObjectBase).
/// - The ordering of disposing the buffer before reading the blocks still in
///   it in the orchestrator's finally: nothing observable lies between; its
///   component-level property is pinned by PersistentObjectBaseCancellationTests.
/// - Cancellation inside ProcessBlockAsync/BuildGraph: no seam; same handling
///   path as the fetch cases.
/// </summary>
[Trait("Category", "Functional")]
public class BitcoinOrchestratorTraverseCancellationTests : TestsBase, IClassFixture<BaselineTraverseFixture>
{
    private const long From = TraverseOptionsBuilder.DefaultFromHeight;
    private const long To = TraverseOptionsBuilder.DefaultToHeight;
    private readonly BaselineTraverseFixture _baseline;

    public BitcoinOrchestratorTraverseCancellationTests(BaselineTraverseFixture baseline) => _baseline = baseline;

    [Fact]
    public async Task TraverseAsync_CanceledWhileWritingSecondBatch_AccountsForEveryBlockExactlyOnce()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(
            Point.WhileWritingBlock,
            TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithMaxEntitiesPerBatch(1),
            From + 1);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Equal(2, (await new TraverseWorkingDir(scenario.Options).BatchesAsync()).Count);
        TraverseAssert.EveryBlockAccountedExactlyOnce(
            scenario.RequestedHeights, await TraverseOutput.ReadAsync(scenario.Options));
    }

    [Theory]
    [InlineData(Point.AfterQueuesPersisted)]
    [InlineData(Point.WhileBufferIsFull)]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileFetchingBlockBody)]
    [InlineData(Point.BlockHashDeliveredAfterCancellation)]
    [InlineData(Point.BlockBodyDeliveredAfterCancellation)]
    [InlineData(Point.WhileWritingBlock)]
    [InlineData(Point.WhileWritingBlockWithBufferedBlocks)]
    [InlineData(Point.WhileFetchingSeveralBlocks)]
    [InlineData(Point.WhileRetryingBlock)]
    public async Task TraverseAsync_Canceled_LeavesConsistentState(Point point)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(point, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        TraverseAssert.CanceledStateIsConsistent(
            scenario.RequestedHeights, await TraverseOutput.ReadAsync(scenario.Options), _baseline.Output);
    }

    [Fact]
    public async Task TraverseAsync_CanceledBeforeFirstBlock_RequestsNothingAndLeavesEveryBlockPending()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(Point.AfterQueuesPersisted, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        Assert.Multiple(
            () => Assert.Empty(scenario.Node.GetRequests(BitcoinCoreEndpoint.BlockHashByHeight)),
            () => Assert.Empty(scenario.Node.GetRequests(BitcoinCoreEndpoint.Block)),
            () => Assert.Empty(output.WrittenBlockHeights),
            () => Assert.Equal(scenario.RequestedHeights, output.GetAccountedHeights()));
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileBufferIsFull_WritesOnlyBufferedBlock()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(Point.WhileBufferIsFull, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Equal([From], (await TraverseOutput.ReadAsync(scenario.Options)).WrittenBlockHeights);
    }

    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileFetchingBlockBody)]
    public async Task TraverseAsync_CanceledWhileFetchingBlock_LeavesThatBlockPending(Point point)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(point, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        Assert.Equal([From, From + 1], output.WrittenBlockHeights);
        Assert.Equal([From + 2, From + 3, From + 4, To], output.ToProcessBlockHeights.Order());
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileFetchingBlock_DoesNotRetryTheRequest()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(Point.WhileFetchingBlockHash, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Equal(1, scenario.Node.CountRequests(BitcoinCoreEndpoint.BlockHashByHeight, From + 2));
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileWritingBlock_WritesThatBlock()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(Point.WhileWritingBlock, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        Assert.Equal([From, From + 1, From + 2], output.WrittenBlockHeights);
        Assert.Equal([From + 3, From + 4, To], output.PendingBlockHeights.Order());
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileWritingBlock_LeavesBufferedBlocksPending()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(
            Point.WhileWritingBlockWithBufferedBlocks,
            TraverseOptionsBuilder.ForWorkingDir(TempExeDir),
            From);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        Assert.Equal([From], output.WrittenBlockHeights);
        Assert.Equal([From + 1, From + 2, From + 3, From + 4, To], output.PendingBlockHeights.Order());
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileWritingLastBlock_WritesEveryBlock()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(Point.WhileWritingLastBlock, TempExeDir);

        // Act
        // The traverse may end normally or be canceled, depending on whether
        // the cancellation precedes the last check of the loop; both are valid.
        await Record.ExceptionAsync(scenario.RunAsync);

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        Assert.Equal(scenario.RequestedHeights, output.WrittenBlockHeights.Order());
        Assert.Empty(output.PendingBlockHeights);
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileRetryingBlock_LeavesBlockPendingOnce()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(Point.WhileRetryingBlock, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        Assert.Single(output.ToProcessBlockHeights, From + 2);
        Assert.DoesNotContain(From + 2, output.FailedBlockHeights);
        Assert.Equal(2, scenario.Node.CountRequests(BitcoinCoreEndpoint.BlockHashByHeight, From + 2));
    }

    [Fact]
    public async Task TraverseAsync_CanceledWithPreviouslyFailedBlocks_AccountsForThemOnce()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(
            Point.WhileFetchingBlockHash,
            TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithPreviouslyFailedBlocks(71050, 71051),
            From + 2);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        TraverseAssert.EveryBlockAccountedExactlyOnce(scenario.RequestedHeights, output);
        Assert.Empty(output.FailedBlockHeights);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TraverseAsync_CanceledWithBlocksList_AccountsForListedBlocksOnly(bool useBlocksListFile)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(
            Point.WhileWritingBlock,
            CreateNonContiguousBuilder(useBlocksListFile),
            From + 2);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Equal(
            scenario.RequestedHeights.Order(),
            (await TraverseOutput.ReadAsync(scenario.Options)).GetAccountedHeights());
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileFetchingFirstBlock_WritesNothing()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(
            Point.WhileFetchingBlockHash, TraverseOptionsBuilder.ForWorkingDir(TempExeDir), From);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Empty((await TraverseOutput.ReadAsync(scenario.Options)).WrittenBlockHeights);
        Assert.Empty(await new TraverseWorkingDir(scenario.Options).BatchFilesAsync());
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileFetchingLastBlock_WritesAllButLast()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(
            Point.WhileFetchingBlockHash, TraverseOptionsBuilder.ForWorkingDir(TempExeDir), To);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        var output = await TraverseOutput.ReadAsync(scenario.Options);
        Assert.Equal([From, From + 1, From + 2, From + 3, From + 4], output.WrittenBlockHeights);
        Assert.Equal([To], output.PendingBlockHeights);
    }

    [Fact]
    public async Task TraverseAsync_CanceledWhileWritingFirstBlock_WritesOnlyThatBlock()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(
            Point.WhileWritingBlock, TraverseOptionsBuilder.ForWorkingDir(TempExeDir), From);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Equal([From], (await TraverseOutput.ReadAsync(scenario.Options)).WrittenBlockHeights);
    }

    [Theory]
    [InlineData(Point.BeforeStart)]
    [InlineData(Point.WhileRequestingChainInfo)]
    public async Task TraverseAsync_CanceledBeforeStart_ThrowsAndCreatesNoFiles(Point point)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(point, TempExeDir);

        // Act
        var exception = await Record.ExceptionAsync(scenario.RunAsync);

        // Assert
        Assert.Multiple(
            () => Assert.IsAssignableFrom<OperationCanceledException>(exception),
            () => Assert.Empty(new TraverseWorkingDir(scenario.Options).FileKinds()));
    }

    [Fact]
    public async Task TraverseAsync_CanceledAfterLastFetch_EndsWithEveryBlockAccounted()
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(Point.AfterLastFetch, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        TraverseAssert.EveryBlockAccountedExactlyOnce(
            scenario.RequestedHeights, await TraverseOutput.ReadAsync(scenario.Options));
    }

    private TraverseOptionsBuilder CreateNonContiguousBuilder(bool useBlocksListFile)
    {
        return useBlocksListFile
            ? TraverseOptionsBuilder.ForWorkingDir(TempExeDir)
                .WithBlocksListFile(From, From + 2, From + 3, To)
            : TraverseOptionsBuilder.ForWorkingDir(TempExeDir)
                .WithGranularity(2);
    }
}
