using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Db.Neo4jDb;

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
    public static async Task SerializeAsync(BitcoinNeo4jDb db, Options options, params long[] heights)
    {
        foreach (var height in heights)
        {
            using var graph = await CreateAsync(height, options);
            await db.SerializeAsync(graph, CancellationToken.None);
        }
    }

    public static async Task<BlockGraph> CreateAsync(
        long height,
        Options options,
        CancellationToken ct = default)
    {
        using var node = SyntheticBitcoinNode.FromMockData();
        using var agent = TestBitcoinAgent.Create(node);

        return await agent.GetGraph(height, options.Bitcoin, ct);
    }
}
