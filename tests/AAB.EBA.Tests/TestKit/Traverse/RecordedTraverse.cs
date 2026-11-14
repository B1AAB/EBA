using AAB.EBA.CLI.Config;
using AAB.EBA.Tests.TestKit.BitcoinCore;

using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

public static class RecordedTraverse
{
    public static async Task<Options> RunAsync(string workingDir, int from, int to)
    {
        var options = TraverseOptionsBuilder.ForWorkingDir(workingDir).WithRange(from, to).Build();
        await TestTraverse.RunRecordedAsync(options);
        return options;
    }

    public static Task RunAsync(Options options) => TestTraverse.RunRecordedAsync(options);
}
