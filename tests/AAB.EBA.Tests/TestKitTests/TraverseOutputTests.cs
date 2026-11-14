using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Graph;
using AAB.EBA.Tests.TestKit.Traverse;

using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace AAB.EBA.Tests.TestKitTests;

[Trait("Category", "Unit")]
public class TraverseOutputTests : TestsBase
{
    [Fact]
    public async Task ReadAsync_HeaderlessBatch_PreservesKindNamesAndBlockAttribution()
    {
        // Arrange
        const long height = TraverseOptionsBuilder.DefaultFromHeight;
        var options = TraverseOptionsBuilder.ForWorkingDir(TempExeDir).Build();
        using (var db = TestGraphDb.Create(options))
            await SyntheticBlockGraph.SerializeAsync(db, options, height);

        // Act
        var output = await TraverseOutput.ReadAsync(options);

        // Assert
        var kind = B2TEdge.Kind.ToString();
        Assert.Multiple(
            () => Assert.Contains(kind, output.Types),
            () => Assert.NotEmpty(output.GetRows(kind)),
            () => Assert.Equal(output.GetRows(kind).Count, output.BlockTables(height)[kind].Rows.Count),
            () => Assert.Contains(output.GetBlockRows(height), row => row.StartsWith(kind + "|")),
            () => Assert.True(output.GetRows(BlockNode.Kind.ToString()).Single().Contains('\t')));
    }
}
