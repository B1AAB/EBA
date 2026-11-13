using AAB.EBA.Tests.TestKit.Traverse;

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

using Point = AAB.EBA.Tests.TestKit.Traverse.TraverseCancellationPoint;

namespace AAB.EBA.Tests.Blockchains.Bitcoin;

/// <summary>
/// The files a canceled <c>BitcoinOrchestrator.TraverseAsync</c> leaves in
/// its working directory; canceled while fetching (nothing being written) or
/// while writing a block.
/// </summary>
[Trait("Category", "Functional")]
public class BitcoinOrchestratorTraverseCancellationArtifactsTests : TestsBase
{
    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    public async Task TraverseAsync_Canceled_LeavesWellFormedStatusFile(Point point)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(point, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        using var status = new TraverseWorkingDir(scenario.Options).StatusFileJson();
        Assert.Equal(JsonValueKind.Object, status.RootElement.ValueKind);
    }

    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    public async Task TraverseAsync_Canceled_LeavesReadableBatchFiles(Point point)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(point, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Empty(await new TraverseWorkingDir(scenario.Options).UnreadableBatchFilesAsync());
    }

    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    public async Task TraverseAsync_Canceled_BatchesListEveryElementType(Point point)
    {
        // Arrange
        var baseline = new TraverseWorkingDir(
            await UncanceledTraverse.RunDefaultAsync(Path.Join(TempExeDir, "baseline")));
        using var scenario = TraverseCancellationScenario.Create(point, Path.Join(TempExeDir, "canceled"));

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Equal(
            await baseline.BatchTypesAsync(),
            await new TraverseWorkingDir(scenario.Options).BatchTypesAsync());
    }

    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    public async Task TraverseAsync_Canceled_ConstantAndSchemaFilesEqualUncanceledTraverse(Point point)
    {
        // Arrange
        var baseline = new TraverseWorkingDir(
            await UncanceledTraverse.RunDefaultAsync(Path.Join(TempExeDir, "baseline")));
        using var scenario = TraverseCancellationScenario.Create(point, Path.Join(TempExeDir, "canceled"));

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Equal(
            baseline.ConstantFiles().OrderBy(x => x.Key),
            new TraverseWorkingDir(scenario.Options).ConstantFiles().OrderBy(x => x.Key));
    }

    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    public async Task TraverseAsync_Canceled_ReleasesEveryFile(Point point)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(point, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Empty(new TraverseWorkingDir(scenario.Options).FilesThatCannotBeOpenedExclusively());
    }

    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    public async Task TraverseAsync_Canceled_LeavesNoUnexpectedFiles(Point point)
    {
        // Arrange
        var baseline = new TraverseWorkingDir(
            await UncanceledTraverse.RunDefaultAsync(Path.Join(TempExeDir, "baseline")));
        using var scenario = TraverseCancellationScenario.Create(point, Path.Join(TempExeDir, "canceled"));

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        Assert.Subset(
            baseline.FileKinds().ToHashSet(),
            new TraverseWorkingDir(scenario.Options).FileKinds().ToHashSet());
    }
}
