using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using AAB.EBA.Tests.TestKit.Graph;
using Xunit;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// Assertions on the output of traverses, with failure messages that name
/// the blocks at fault.
/// </summary>
public static class TraverseAssert
{
    public static void CanceledStateIsConsistent(IEnumerable<long> requested, TraverseOutput actual, TraverseOutput baseline)
    {
        Assert.Multiple(
            () => EveryBlockAccountedExactlyOnce(requested, actual),
            () => Assert.Empty(actual.FailedBlockHeights),
            () => BlocksEqual(baseline, actual, actual.WrittenBlockHeights),
            () =>
            {
                if (actual.WrittenBlockHeights.Count > 0)
                    Assert.Equal(baseline.GetSharedNodeRowCountsPerWrittenBlock(), actual.GetSharedNodeRowCountsPerWrittenBlock());
            });
    }

    public static void ResumedOutputIsComplete(IEnumerable<long> requested, TraverseOutput actual, TraverseOutput baseline)
    {
        Assert.Multiple(
            () => Assert.Equal(requested.Order(), actual.WrittenBlockHeights.Order()),
            () => Assert.Empty(actual.PendingBlockHeights),
            () => OutputsEqual(baseline, actual));
    }

    public static async Task CanceledArtifactsAreWellFormed(TraverseWorkingDir workingDir, TraverseWorkingDir baselineDir)
    {
        var unreadable = await workingDir.UnreadableBatchFilesAsync();
        var expectedTypes = await baselineDir.BatchTypesAsync();
        var actualTypes = await workingDir.BatchTypesAsync();
        Assert.Multiple(
            () =>
            {
                using var status = workingDir.StatusFileJson();
                Assert.Equal(JsonValueKind.Object, status.RootElement.ValueKind);
            },
            () => Assert.Empty(unreadable),
            () => Assert.Equal(expectedTypes, actualTypes),
            () => Assert.Equal(baselineDir.ConstantFiles().OrderBy(x => x.Key), workingDir.ConstantFiles().OrderBy(x => x.Key)),
            () => Assert.Empty(workingDir.FilesThatCannotBeOpenedExclusively()),
            () => Assert.Subset(baselineDir.FileKinds().ToHashSet(), workingDir.FileKinds().ToHashSet()));
    }

    /// <summary>
    /// Every requested block is either written or pending, and none is
    /// written or pending more than once.
    /// </summary>
    public static void EveryBlockAccountedExactlyOnce(IEnumerable<long> requested, TraverseOutput output)
    {
        Assert.True(
            output.GetUnaccountedHeights(requested).Count == 0,
            $"Blocks neither written nor pending: {string.Join(", ", output.GetUnaccountedHeights(requested))}.");

        Assert.True(
            output.GetHeightsAccountedMoreThanOnce().Count == 0,
            $"Blocks accounted for more than once: {string.Join(", ", output.GetHeightsAccountedMoreThanOnce())}.");
    }

    /// <summary>
    /// The rows written for each of the given blocks are the same in both outputs.
    /// </summary>
    public static void BlocksEqual(TraverseOutput expected, TraverseOutput actual, IEnumerable<long> heights)
    {
        foreach (var height in heights)
            GraphCsvAssert.Equal(expected.BlockTables(height), actual.BlockTables(height));
    }

    /// <summary>
    /// Both outputs contain the same rows.
    /// </summary>
    public static void OutputsEqual(TraverseOutput expected, TraverseOutput actual)
    {
        GraphCsvAssert.Equal(expected.Tables, actual.Tables);
    }
}
