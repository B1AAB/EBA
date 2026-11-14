using AAB.EBA.Tests.TestKit.Traverse;

using System;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit;

/// <summary>
/// A one-shot event that tests and test doubles use to coordinate with each
/// other (e.g., "block written", "request received"). Continuations run
/// asynchronously, so setting a signal never runs the code awaiting it on
/// the thread of the code setting it.
/// </summary>
public sealed class Signal
{
    private readonly TaskCompletionSource _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes when the signal is set.
    /// </summary>
    public Task Task => _tcs.Task;

    public bool IsSet => _tcs.Task.IsCompleted;

    /// <summary>
    /// Sets the signal; setting a signal that is already set has no effect.
    /// </summary>
    public void Set() => _tcs.TrySetResult();

    /// <summary>
    /// Waits for the signal to be set, and throws a <see cref="TimeoutException"/>
    /// if it is not set within <paramref name="timeout"/> (default:
    /// <see cref="TraverseRunner.DefaultTimeout"/>), so that a missed event
    /// fails the test instead of hanging it.
    /// </summary>
    public Task WaitAsync(TimeSpan? timeout = null)
    {
        return _tcs.Task.WaitAsync(timeout ?? TraverseRunner.DefaultTimeout);
    }
}
