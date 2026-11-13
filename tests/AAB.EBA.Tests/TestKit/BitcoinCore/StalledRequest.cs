using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

/// <summary>
/// The requests a <see cref="RequestRule"/> stalls: they are not answered
/// until their caller cancels them. Lets tests keep a client busy at a known
/// point, and observe when it gives up.
/// </summary>
public sealed class StalledRequest
{
    private readonly Signal _received = new();
    private readonly Signal _canceled = new();

    private readonly object _lock = new();
    private readonly List<(int Count, Signal Signal)> _countWaiters = [];
    private int _receivedCount;

    /// <summary>
    /// Completes when the first stalled request is received.
    /// </summary>
    public Task Received => _received.Task;

    /// <summary>
    /// Completes when the first stalled request is canceled by its caller,
    /// i.e., when the client has observed the cancellation.
    /// </summary>
    public Task Canceled => _canceled.Task;

    public int ReceivedCount
    {
        get
        {
            lock (_lock)
                return _receivedCount;
        }
    }

    /// <summary>
    /// Completes when at least <paramref name="count"/> stalled requests are received.
    /// </summary>
    public Task WhenReceived(int count)
    {
        lock (_lock)
        {
            if (_receivedCount >= count)
                return Task.CompletedTask;

            var signal = new Signal();
            _countWaiters.Add((count, signal));
            return signal.Task;
        }
    }

    internal async Task StallUntilCanceledAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            _receivedCount++;
            foreach (var waiter in _countWaiters.FindAll(x => x.Count <= _receivedCount))
            {
                waiter.Signal.Set();
                _countWaiters.Remove(waiter);
            }
        }

        _received.Set();

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _canceled.Set();
            throw;
        }
    }
}
