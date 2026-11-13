using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// Assertions on the output of traverses, with failure messages that name
/// the blocks at fault.
/// </summary>
public static class TraverseAssert
{
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
            Assert.True(
                expected.GetBlockRows(height).SequenceEqual(actual.GetBlockRows(height)),
                $"The rows written for block {height} differ from the expected rows.");
    }

    /// <summary>
    /// Both outputs contain the same rows.
    /// </summary>
    public static void OutputsEqual(TraverseOutput expected, TraverseOutput actual)
    {
        Assert.Equal(expected.GetAllRows(), actual.GetAllRows());
    }
}
