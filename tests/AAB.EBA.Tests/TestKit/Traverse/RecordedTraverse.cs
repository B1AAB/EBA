using AAB.EBA.CLI.Config;
using AAB.EBA.Tests.TestKit.BitcoinCore;

using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

public static class RecordedTraverse
{
    public static async Task<Options> RunAsync(string workingDir, int from, int to)
    {
        using var node = RecordedBitcoinNode.FromMockData();
        var options = TraverseOptionsBuilder.ForWorkingDir(workingDir).WithRange(from, to).Build();
        await new TraverseRunner(options, node).RunAsync(CancellationToken.None);
        return options;
    }
}
