using AAB.EBA.CLI.Config;
using AAB.EBA.Tests.TestKit.BitcoinCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// Runs traverses over a <see cref="SyntheticBitcoinNode"/> that are not
/// canceled: as a baseline to compare the output of canceled traverses
/// with, or to resume a canceled traverse.
/// </summary>
public static class UncanceledTraverse
{
    /// <summary>
    /// Runs a traverse with the given options to its end. To resume a traverse,
    /// pass the options of that traverse (i.e., the same working directory).
    /// </summary>
    public static async Task RunAsync(Options options)
    {
        await TestTraverse.RunSyntheticAsync(options);
    }

    /// <summary>
    /// Runs a traverse of the default range in <paramref name="workingDir"/>
    /// to its end, and returns its working directory options.
    /// </summary>
    public static async Task<Options> RunDefaultAsync(string workingDir)
    {
        var options = TraverseOptionsBuilder.ForWorkingDir(workingDir).Build();
        await RunAsync(options);
        return options;
    }

    /// <summary>
    /// Runs a traverse of the default range in a sub-directory of
    /// <paramref name="parentDir"/> to its end, and returns its output.
    /// </summary>
    public static async Task<TraverseOutput> RunBaselineAsync(string parentDir)
    {
        return await TraverseOutput.ReadAsync(
            await RunDefaultAsync(System.IO.Path.Join(parentDir, "baseline_" + Guid.NewGuid().ToString("N"))));
    }

}
