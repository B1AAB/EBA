using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AAB.EBA.Tests.TestKit.Traverse;

public static class SnapshotExtensions
{
    /// <summary>
    /// The entries of a <see cref="TraverseWorkingDir.Snapshot"/> for the given
    /// files (full paths or names), sorted by file name.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, string>> FilteredTo(
        this IReadOnlyDictionary<string, string> snapshot,
        IEnumerable<string> files)
    {
        var names = files.Select(Path.GetFileName).ToHashSet();
        return [.. snapshot.Where(x => names.Contains(x.Key)).OrderBy(x => x.Key, System.StringComparer.Ordinal)];
    }
}
