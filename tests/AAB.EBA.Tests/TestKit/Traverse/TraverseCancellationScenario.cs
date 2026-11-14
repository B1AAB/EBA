using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Graph;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// A Bitcoin traverse over a <see cref="SyntheticBitcoinNode"/> that is
/// canceled at a precise, known point of its progress (see
/// <see cref="TraverseCancellationPoint"/>). The point of cancellation is
/// reached through events of the traverse (a request received by the node,
/// a block being written, etc.), not by time, so the scenario is deterministic.
///
/// Hooks that run inside the write of a block never throw (an exception there
/// terminates the process; see PersistentObjectBase), and they await events
/// without a timeout: if an event never happens, the traverse does not end and
/// <see cref="TraverseRunner"/> fails the test with its timeout.
/// </summary>
public sealed class TraverseCancellationScenario : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<long, Signal> _written = new();
    private readonly List<Func<long, Task>> _onWriting = [];
    private TraverseRunner _runner = null!;

    public Options Options { get; private set; } = null!;
    public SyntheticBitcoinNode Node { get; } = SyntheticBitcoinNode.FromMockData();

    /// <summary>
    /// The heights the traverse is requested to process.
    /// </summary>
    public IReadOnlyList<long> RequestedHeights { get; private set; } = [];

    private TraverseCancellationScenario() { }

    /// <summary>
    /// Creates a scenario where the traverse with the options of
    /// <paramref name="optionsBuilder"/> is canceled at <paramref name="point"/>
    /// about the block at <paramref name="height"/> (ignored by the points that
    /// are not about a block). The scenario may change the options the point
    /// requires (e.g., the size of the buffer), then builds them.
    /// </summary>
    public static TraverseCancellationScenario Create(
        TraverseCancellationPoint point,
        TraverseOptionsBuilder optionsBuilder,
        long height)
    {
        var scenario = new TraverseCancellationScenario();
        scenario.Setup(point, optionsBuilder, height);
        return scenario;
    }

    /// <summary>
    /// Creates a scenario for the given point using the default range of
    /// <see cref="TraverseOptionsBuilder"/> and a default block for the point.
    /// </summary>
    public static TraverseCancellationScenario Create(
        TraverseCancellationPoint point,
        string workingDir)
    {
        return Create(point, TraverseOptionsBuilder.ForWorkingDir(workingDir), GetDefaultHeight(point));
    }

    /// <summary>
    /// The block a point is about when none is given: a block in the middle of
    /// the default range, so there are blocks before and after it; or the last
    /// block for the points about the last block.
    /// </summary>
    public static long GetDefaultHeight(TraverseCancellationPoint point)
    {
        return point switch
        {
            TraverseCancellationPoint.WhileWritingLastBlock => TraverseOptionsBuilder.DefaultToHeight,
            TraverseCancellationPoint.AfterLastFetch => TraverseOptionsBuilder.DefaultFromHeight,
            TraverseCancellationPoint.WhileBufferIsFull => TraverseOptionsBuilder.DefaultFromHeight,
            _ => TraverseOptionsBuilder.DefaultFromHeight + 2
        };
    }

    /// <summary>
    /// Runs the traverse until it is canceled; see
    /// <see cref="TraverseRunner.RunUntilCanceledAsync"/>.
    /// </summary>
    public Task RunUntilCanceledAsync() => _runner.RunUntilCanceledAsync(_cts.Token);

    /// <summary>
    /// Runs the traverse and throws whatever it throws; see
    /// <see cref="TraverseRunner.RunAsync"/>.
    /// </summary>
    public Task RunAsync() => _runner.RunAsync(_cts.Token);

    private void Setup(TraverseCancellationPoint point, TraverseOptionsBuilder builder, long height)
    {
        Action? onGraphDbResolved = null;

        switch (point)
        {
            case TraverseCancellationPoint.BeforeStart:
                _cts.Cancel();
                break;

            case TraverseCancellationPoint.WhileRequestingChainInfo:
                CancelOn(Node.On(BitcoinCoreEndpoint.ChainInfo).Stall().Received);
                break;

            case TraverseCancellationPoint.AfterQueuesPersisted:
                onGraphDbResolved = _cts.Cancel;
                break;

            case TraverseCancellationPoint.WhileBufferIsFull:
                builder.WithMaxBlocksInBuffer(1);
                OnWriting(height, () =>
                {
                    _cts.Cancel();
                    return Task.CompletedTask;
                });
                break;

            case TraverseCancellationPoint.WhileFetchingBlockHash:
            case TraverseCancellationPoint.WhileFetchingBlockBody:
                {
                    var stall = Node.On(ToEndpoint(point), height).Stall();
                    CancelOn(Task.WhenAll(stall.Received, PreviousBlockWritten(builder, height)));
                    break;
                }

            case TraverseCancellationPoint.BlockHashDeliveredAfterCancellation:
            case TraverseCancellationPoint.BlockBodyDeliveredAfterCancellation:
                {
                    var previousWritten = PreviousBlockWritten(builder, height);
                    Node.On(ToEndpoint(point), height).Before(async _ =>
                    {
                        await previousWritten;
                        _cts.Cancel();
                    });
                    break;
                }

            case TraverseCancellationPoint.WhileWritingBlock:
                {
                    var stall = Node.On(BitcoinCoreEndpoint.BlockHashByHeight, NextRequested(builder, height, 1)).Stall();
                    OnWriting(height, async () =>
                    {
                        await stall.Received;
                        _cts.Cancel();
                        await stall.Canceled;
                    });
                    break;
                }

            case TraverseCancellationPoint.WhileWritingBlockWithBufferedBlocks:
                {
                    var stall = Node.On(BitcoinCoreEndpoint.BlockHashByHeight, NextRequested(builder, height, 3)).Stall();
                    OnWriting(height, async () =>
                    {
                        await stall.Received;
                        _cts.Cancel();
                        await stall.Canceled;
                    });
                    break;
                }

            case TraverseCancellationPoint.WhileWritingLastBlock:
                OnWriting(height, () =>
                {
                    _cts.Cancel();
                    return Task.CompletedTask;
                });
                break;

            case TraverseCancellationPoint.AfterLastFetch:
                {
                    var lastBodyServed = new Signal();
                    Node.On(BitcoinCoreEndpoint.Block, builder.RequestedHeights[^1]).Before(_ =>
                    {
                        lastBodyServed.Set();
                        return Task.CompletedTask;
                    });
                    OnWriting(height, async () =>
                    {
                        await lastBodyServed.Task;
                        _cts.Cancel();
                    });
                    break;
                }

            case TraverseCancellationPoint.WhileFetchingSeveralBlocks:
                builder.WithMaxConcurrentBlocks(3);
                CancelOn(Task.WhenAll(
                    Node.On(BitcoinCoreEndpoint.BlockHashByHeight, height).Stall().Received,
                    PreviousBlockWritten(builder, height)));
                break;

            case TraverseCancellationPoint.WhileRetryingBlock:
                builder.WithAgentRetry(3, TimeSpan.FromMilliseconds(1));
                CancelOn(Node.On(BitcoinCoreEndpoint.BlockHashByHeight, height).Fail(1).Stall().Received);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(point), point, null);
        }

        RequestedHeights = builder.RequestedHeights;
        Options = builder.Build();
        _runner = new TraverseRunner(Options, Node, inner =>
        {
            var graphDb = new InterceptingGraphDb<BitcoinGraph>(inner)
            {
                OnSerializingAsync = async graph =>
                {
                    var h = GetHeight(graph);
                    foreach (var hook in _onWriting)
                        await hook(h);
                },
                OnSerializedAsync = graph =>
                {
                    GetWrittenSignal(GetHeight(graph)).Set();
                    return Task.CompletedTask;
                }
            };
            return graphDb;
        })
        {
            OnGraphDbResolved = onGraphDbResolved
        };
    }

    private void OnWriting(long height, Func<Task> hook)
    {
        _onWriting.Add(h => h == height ? hook() : Task.CompletedTask);
    }

    private void CancelOn(Task task)
    {
        _ = task.ContinueWith(
            _ => _cts.Cancel(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    /// <summary>
    /// Completes when the block requested right before <paramref name="height"/>
    /// is written; completed already when there is no such block.
    /// </summary>
    private Task PreviousBlockWritten(TraverseOptionsBuilder builder, long height)
    {
        var requested = builder.RequestedHeights.ToList();
        var index = requested.IndexOf(height);
        return index > 0
            ? GetWrittenSignal(requested[index - 1]).Task
            : Task.CompletedTask;
    }

    /// <summary>
    /// The height requested <paramref name="offset"/> places after <paramref name="height"/>.
    /// </summary>
    private static long NextRequested(TraverseOptionsBuilder builder, long height, int offset)
    {
        var requested = builder.RequestedHeights.ToList();
        var index = requested.IndexOf(height) + offset;
        if (index < offset || index >= requested.Count)
            throw new ArgumentOutOfRangeException(
                nameof(height),
                $"There must be {offset} requested block(s) after block {height}.");
        return requested[index];
    }

    private Signal GetWrittenSignal(long height)
    {
        return _written.GetOrAdd(height, _ => new Signal());
    }

    private static BitcoinCoreEndpoint ToEndpoint(TraverseCancellationPoint point)
    {
        return point is TraverseCancellationPoint.WhileFetchingBlockBody
            or TraverseCancellationPoint.BlockBodyDeliveredAfterCancellation
            ? BitcoinCoreEndpoint.Block
            : BitcoinCoreEndpoint.BlockHashByHeight;
    }

    private static long GetHeight(BitcoinGraph graph)
    {
        return ((BlockGraph)graph).Block.Height;
    }

    public void Dispose()
    {
        Node.Dispose();
        _cts.Dispose();
    }
}
