using AAB.EBA.Tests.TestKit;
using AAB.EBA.Tests.TestKit.PersistentObject;

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests.PersistentObject;

/// <summary>
/// Not testable from here: that items enqueued after cancellation are never
/// taken while the listener is idle (no way to know the listener has stopped).
/// </summary>
[Trait("Category", "Unit")]
public class PersistentObjectBaseCancellationTests
{
    [Fact]
    public async Task Listener_CanceledWhileSerializing_CompletesItemInFlight()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var obj = new GatedPersistentObject(cts.Token);
        obj.Enqueue(1);
        await obj.Started(1).WaitAsync(TimeSpan.FromSeconds(30));
        cts.Cancel();

        // Act
        obj.Release(1);
        obj.Dispose();

        // Assert
        Assert.Equal([1], obj.SerializedItems);
    }

    [Fact]
    public async Task Listener_CanceledWhileSerializing_DoesNotSerializeQueuedItems()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var obj = new GatedPersistentObject(cts.Token);
        obj.Enqueue(1);
        await obj.Started(1).WaitAsync(TimeSpan.FromSeconds(30));
        obj.Enqueue(2);
        obj.Release(2);
        cts.Cancel();

        // Act
        obj.Release(1);
        obj.Dispose();

        // Assert
        Assert.DoesNotContain(2, obj.SerializedItems);
    }

    [Fact]
    public async Task Dispose_WhileSerializing_ReturnsAfterItemIsSerialized()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var obj = new GatedPersistentObject(cts.Token);
        obj.Enqueue(1);
        await obj.Started(1).WaitAsync(TimeSpan.FromSeconds(30));
        cts.Cancel();

        // Act
        var dispose = Task.Run(obj.Dispose);

        // Assert
        // This bounded wait can only make the test pass falsely (if Dispose
        // were to return later than this), never fail it falsely.
        Assert.False(await dispose.CompletesWithinAsync(TimeSpan.FromMilliseconds(300)));
        obj.Release(1);
        await dispose.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal([1], obj.SerializedItems);
    }
}
