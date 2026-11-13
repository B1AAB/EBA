using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.CLI.Config;
using AAB.EBA.Tests.TestKit;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Traverse;

using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests.Blockchains.Bitcoin;

[Trait("Category", "Unit")]
public class BitcoinChainAgentCancellationTests
{
    private const long Height = TraverseOptionsBuilder.DefaultFromHeight;

    [Fact]
    public async Task GetGraph_TokenAlreadyCanceled_ThrowsWithoutRequesting()
    {
        // Arrange
        using var node = SyntheticBitcoinNode.FromMockData();
        using var agent = CreateAgent(node);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => agent.GetGraph(Height, new BitcoinOptions(), cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
        Assert.Empty(node.Requests);
    }

    [Theory]
    [InlineData(BitcoinCoreEndpoint.BlockHashByHeight)]
    [InlineData(BitcoinCoreEndpoint.Block)]
    public async Task GetGraph_CanceledWhileFetching_ThrowsOperationCanceledException(BitcoinCoreEndpoint endpoint)
    {
        // Arrange
        using var node = SyntheticBitcoinNode.FromMockData();
        using var agent = CreateAgent(node);
        using var cts = new CancellationTokenSource();
        var stall = node.On(endpoint).Stall();
        _ = stall.Received.ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);

        // Act
        var act = () => agent.GetGraph(Height, new BitcoinOptions(), cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task AssertChainAsync_Canceled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var node = SyntheticBitcoinNode.FromMockData();
        using var agent = CreateAgent(node);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => agent.AssertChainAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task IsConnectedAsync_CanceledWhileRequestingChainInfo_ThrowsOperationCanceledException()
    {
        // Arrange
        using var node = SyntheticBitcoinNode.FromMockData();
        var stall = node.On(BitcoinCoreEndpoint.ChainInfo).Stall();
        using var agent = CreateAgent(node);
        using var cts = new CancellationTokenSource();
        _ = stall.Received.ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);

        // Act
        var act = () => agent.IsConnectedAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task IsConnectedAsync_ClientUnreachable_ReturnsNotConnected()
    {
        // Arrange
        using var node = SyntheticBitcoinNode.FromMockData();
        node.On(BitcoinCoreEndpoint.ChainInfo).Fail(1);
        using var agent = CreateAgent(node);

        // Act
        var (isConnected, chainInfo) = await agent.IsConnectedAsync(CancellationToken.None);

        // Assert
        Assert.False(isConnected);
        Assert.Null(chainInfo);
    }

    private static BitcoinChainAgent CreateAgent(SyntheticBitcoinNode node)
    {
        return new BitcoinChainAgent(
            new HttpClient(node, disposeHandler: false)
            {
                BaseAddress = new BitcoinTraverseOptions().ClientUri
            },
            NullLogger<BitcoinChainAgent>.Instance);
    }
}
