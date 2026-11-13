using System.Collections.Generic;
using System.Linq;

namespace AAB.EBA.Tests.TestKit.Traverse;

public static class HeightListExtensions
{
    /// <summary>
    /// The heights in ascending order (blocks can be pending or written in
    /// any order, e.g., when re-queued on cancellation).
    /// </summary>
    public static IReadOnlyList<long> Sorted(this IEnumerable<long> heights)
    {
        return [.. heights.Order()];
    }
}
