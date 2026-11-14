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
public class BitcoinOrchestratorTraverseCancellationArtifactsTests : TestsBase, IClassFixture<BaselineTraverseFixture>
{
    private readonly BaselineTraverseFixture _baseline;

    public BitcoinOrchestratorTraverseCancellationArtifactsTests(BaselineTraverseFixture baseline) => _baseline = baseline;

    [Theory]
    [InlineData(Point.WhileFetchingBlockHash)]
    [InlineData(Point.WhileWritingBlock)]
    public async Task TraverseAsync_Canceled_LeavesWellFormedArtifacts(Point point)
    {
        // Arrange
        using var scenario = TraverseCancellationScenario.Create(point, TempExeDir);

        // Act
        await scenario.RunUntilCanceledAsync();

        // Assert
        await TraverseAssert.CanceledArtifactsAreWellFormed(new TraverseWorkingDir(scenario.Options), _baseline.WorkingDir);
    }
}
