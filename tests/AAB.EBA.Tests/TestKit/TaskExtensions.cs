using System;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit;

public static class TaskExtensions
{
    /// <summary>
    /// True if <paramref name="task"/> completes within <paramref name="timeout"/>.
    /// </summary>
    public static async Task<bool> CompletesWithinAsync(this Task task, TimeSpan timeout)
    {
        return await Task.WhenAny(task, Task.Delay(timeout)) == task;
    }
}
