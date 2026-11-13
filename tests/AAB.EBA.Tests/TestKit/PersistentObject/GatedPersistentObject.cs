using AAB.EBA.PersistentObject;

using Microsoft.Extensions.Logging.Abstractions;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.PersistentObject;

/// <summary>
/// A <see cref="PersistentObjectBase{T}"/> whose serialization of an item
/// signals that it started, then waits until the test opens the gate of that
/// item. Records the items it serialized. Never throws (an exception in the
/// listener terminates the process).
/// </summary>
public sealed class GatedPersistentObject(CancellationToken ct)
    : PersistentObjectBase<int>(NullLogger<PersistentObjectBase<int>>.Instance, ct)
{
    private readonly ConcurrentDictionary<int, Signal> _started = new();
    private readonly ConcurrentDictionary<int, Signal> _gates = new();
    private readonly ConcurrentQueue<int> _serialized = new();

    public IReadOnlyList<int> SerializedItems => [.. _serialized];

    /// <summary>
    /// Completes when the serialization of <paramref name="item"/> starts.
    /// </summary>
    public Task Started(int item) => GetSignal(_started, item).Task;

    /// <summary>
    /// Lets the serialization of <paramref name="item"/> complete.
    /// </summary>
    public void Release(int item) => GetSignal(_gates, item).Set();

    public override async Task SerializeAsync(int obj, CancellationToken cT)
    {
        GetSignal(_started, obj).Set();
        await GetSignal(_gates, obj).Task;
        _serialized.Enqueue(obj);
    }

    public override async Task SerializeAsync(IEnumerable<int> objs, CancellationToken cT)
    {
        foreach (var obj in objs)
            await SerializeAsync(obj, cT);
    }

    private static Signal GetSignal(ConcurrentDictionary<int, Signal> signals, int item)
    {
        return signals.GetOrAdd(item, _ => new Signal());
    }
}
