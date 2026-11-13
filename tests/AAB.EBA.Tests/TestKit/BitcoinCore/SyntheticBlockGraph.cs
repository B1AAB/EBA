using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;

using Microsoft.Extensions.Logging.Abstractions;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

/// <summary>
/// Creates <see cref="BlockGraph"/> instances the same way a traverse does,
/// from the blocks served by a <see cref="SyntheticBitcoinNode"/>.
/// </summary>
public static class SyntheticBlockGraph
{
    public static async Task<BlockGraph> CreateAsync(
        long height,
        Options options,
        CancellationToken ct = default)
    {
        using var node = SyntheticBitcoinNode.FromMockData();
        using var client = new HttpClient(node, disposeHandler: false)
        {
            BaseAddress = options.Bitcoin.Traverse.ClientUri
        };
        using var agent = new BitcoinChainAgent(client, NullLogger<BitcoinChainAgent>.Instance);

        return await agent.GetGraph(height, options.Bitcoin, ct);
    }
}
