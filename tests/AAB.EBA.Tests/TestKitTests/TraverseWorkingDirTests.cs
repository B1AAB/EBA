using AAB.EBA.Tests.TestKit.Traverse;

using System.IO;
using Xunit;

namespace AAB.EBA.Tests.TestKitTests;

[Trait("Category", "Unit")]
public class TraverseWorkingDirTests : TestsBase
{
    [Theory]
    [InlineData("1794456334_nodes_Block.csv.gz", "_nodes_Block.csv.gz")]
    [InlineData("1794456334_1333_nodes_Block.csv.gz", "_nodes_Block.csv.gz")]
    [InlineData("1794456334_bitcoin_blocks_to_process.eba", "_bitcoin_blocks_to_process.eba")]
    [InlineData("header_nodes_Block.csv.gz", "header_nodes_Block.csv.gz")]
    public void FileKinds_LegacyAndCurrentFilenames_RemovesOnlyTheBatchPrefix(string filename, string expected)
    {
        // Arrange
        var options = TraverseOptionsBuilder.ForWorkingDir(TempExeDir).Build();
        File.WriteAllText(Path.Join(TempExeDir, filename), "");

        // Act
        var kinds = new TraverseWorkingDir(options).FileKinds();

        // Assert
        Assert.Equal(expected, Assert.Single(kinds));
    }
}
