using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.PersistentObject;
using AAB.EBA.Tests.TestKit;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Traverse;

using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests.PersistentObject;

/// <summary>
/// The buffers here are built with an already-canceled token, so their
/// listener exits at once and never takes a block: blocks are either
/// serialized by a direct call, or stay in the buffer.
/// </summary>
[Trait("Category", "Unit")]
public class PersistentGraphBufferTests : TestsBase
{
	private const int Height = TraverseOptionsBuilder.DefaultFromHeight;

	[Fact]
	public async Task SerializeAsync_WhenCanceled_WritesTheBlock()
	{
		// Arrange
		var options = CreateOptions();
		var graph = await SyntheticBlockGraph.CreateAsync(Height, options);
		using var cts = CreateCanceledTokenSource();
		using var buffer = CreateBuffer(options, new SemaphoreSlim(0, 1), cts.Token);

		// Act
		await buffer.SerializeAsync(graph, cts.Token);
		buffer.Dispose();

		// Assert
		var output = await TraverseOutput.ReadAsync(options);
		Assert.Contains(Height, output.WrittenBlockHeights);
	}

	[Fact]
	public async Task SerializeAsync_WhenCanceled_RemovesBlockFromBuffer()
	{
		// Arrange
		var options = CreateOptions();
		var graph = await SyntheticBlockGraph.CreateAsync(Height, options);
		using var cts = CreateCanceledTokenSource();
		using var buffer = CreateBuffer(options, new SemaphoreSlim(0, 1), cts.Token);
		buffer.Enqueue(graph);

		// Act
		await buffer.SerializeAsync(graph, cts.Token);

		// Assert
		Assert.Empty(buffer.BlocksHeightInBuffer);
	}

	[Fact]
	public async Task SerializeAsync_WhenCanceled_ReleasesSemaphoreOnce()
	{
		// Arrange
		var options = CreateOptions();
		var graph = await SyntheticBlockGraph.CreateAsync(Height, options);
		using var cts = CreateCanceledTokenSource();
		using var semaphore = new SemaphoreSlim(0, 1);
		using var buffer = CreateBuffer(options, semaphore, cts.Token);

		// Act
		await buffer.SerializeAsync(graph, cts.Token);

		// Assert
		Assert.Equal(1, semaphore.CurrentCount);
	}

	[Fact]
	public async Task Enqueue_AfterCancellation_KeepsBlockInBuffer()
	{
		// Arrange
		var options = CreateOptions();
		var graph = await SyntheticBlockGraph.CreateAsync(Height, options);
		using var cts = CreateCanceledTokenSource();
		using var buffer = CreateBuffer(options, new SemaphoreSlim(0, 1), cts.Token);

		// Act
		buffer.Enqueue(graph);
		buffer.Dispose();

		// Assert
		Assert.Contains(Height, buffer.BlocksHeightInBuffer);
	}

	[Fact]
	public async Task WaitForBufferToEmptyAsync_CanceledWithBlocksInBuffer_ThrowsOperationCanceledException()
	{
		// Arrange
		var options = CreateOptions();
		var graph = await SyntheticBlockGraph.CreateAsync(Height, options);
		using var cts = CreateCanceledTokenSource();
		using var buffer = CreateBuffer(options, new SemaphoreSlim(0, 1), cts.Token);
		buffer.Enqueue(graph);

		// Act
		var act = () => buffer.WaitForBufferToEmptyAsync(cts.Token).WaitAsync(TraverseRunner.DefaultTimeout);

		// Assert
		await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
	}

	private Options CreateOptions()
	{
		return TraverseOptionsBuilder.ForWorkingDir(TempExeDir).WithRange(Height, Height).Build();
	}

	private static CancellationTokenSource CreateCanceledTokenSource()
	{
		var cts = new CancellationTokenSource();
		cts.Cancel();
		return cts;
	}

	private static PersistentGraphBuffer CreateBuffer(
		Options options, SemaphoreSlim semaphore, CancellationToken ct)
	{
		var graphOrchestrator = new BitcoinGraphOrchestrator(
			options,
			new BitcoinNeo4jDb(options, NullLogger<Neo4jDb<BitcoinGraph>>.Instance),
			NullLogger<BitcoinGraphOrchestrator>.Instance);

		return new PersistentGraphBuffer(
			graphOrchestrator,
			NullLogger<PersistentGraphBuffer>.Instance,
			options.Bitcoin.Traverse.MaxTxoPerFile,
			semaphore,
			options,
			ct);
	}
}
