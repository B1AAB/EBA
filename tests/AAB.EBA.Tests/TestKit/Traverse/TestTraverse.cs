using AAB.EBA.CLI.Config;
using AAB.EBA.Tests.TestKit.BitcoinCore;

using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

internal static class TestTraverse
{
    internal static Task RunRecordedAsync(Options options) => RunAsync(options, RecordedBitcoinNode.FromMockData());
    internal static Task RunSyntheticAsync(Options options) => RunAsync(options, SyntheticBitcoinNode.FromMockData());

    private static async Task RunAsync(Options options, BitcoinNodeStub node)
    {
        using (node)
            await new TraverseRunner(options, node).RunAsync(CancellationToken.None);
    }
}
