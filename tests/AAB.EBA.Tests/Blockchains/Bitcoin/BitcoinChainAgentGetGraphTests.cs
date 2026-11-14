using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Tests.TestKit.BitcoinCore;

using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace AAB.EBA.Tests.Blockchains.Bitcoin;

[Trait("Category", "Unit")]
public class BitcoinChainAgentGetGraphTests
{
    [Theory]
    [InlineData(71036)]
    [InlineData(71037)]
    [InlineData(132317)]
    [InlineData(717531)]
    public async Task GetGraph_RecordedBlock_MatchesRecordedBlock(int height)
    {
        // Arrange
        using var node = RecordedBitcoinNode.FromMockData();
        using var agent = TestBitcoinAgent.Create(node);
        var block = RecordedBlock.FromMockData(height);

        // Act
        using var graph = await agent.GetGraph(height, new BitcoinOptions(), CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(block.Height, graph.Block.Height),
            () => Assert.Equal(block.Hash, graph.Block.Hash),
            () => Assert.Equal(ExpectedCounts(block).Where(x => x.Value > 0).OrderBy(x => x.Key),
                graph.EdgesByType.ToDictionary(x => x.Key.ToString(), x => x.Value.Count).OrderBy(x => x.Key)),
            () => Assert.Single(graph.NodesByType[TxNode.Kind].OfType<TxNode>(), tx => tx.Txid == block.Coinbase.Id),
            () => Assert.Equal(ExpectedTransactionIds(block), graph.NodesByType[TxNode.Kind]
                .Select(node => Assert.IsType<TxNode>(node).Txid).Order(StringComparer.Ordinal)));
    }

    private static IEnumerable<string> ExpectedTransactionIds(RecordedBlock block) =>
        block.Transactions.Select(tx => tx.Id)
            .Concat(block.Transactions.SelectMany(tx => tx.SourceTransactionIds))
            .Distinct().Order(StringComparer.Ordinal);

    private static Dictionary<string, int> ExpectedCounts(RecordedBlock block) => new()
    {
        [B2TEdge.Kind.ToString()] = block.Transactions.Count,
        [C2TEdge.Kind.ToString()] = 1,
        [S2TEdge.Kind.ToString()] = block.Transactions.Sum(x => x.InputCount),
        [T2SEdge.Kind.ToString()] = block.Transactions.Sum(x => x.OutputCount),
        [T2TEdge.KindFee.ToString()] = block.Transactions.Count - 1,
        [T2TEdge.KindTransfers.ToString()] = block.Transactions.Sum(x => x.SourceTransactionIds.Distinct().Count())
    };
}
