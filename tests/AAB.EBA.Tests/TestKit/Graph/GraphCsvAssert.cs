using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Xunit;

namespace AAB.EBA.Tests.TestKit.Graph;

public static class GraphCsvAssert
{
    public static void Equal(IReadOnlyDictionary<string, GraphCsv> expected,
        IReadOnlyDictionary<string, GraphCsv> actual)
    {
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));
        foreach (var type in expected.Keys.Order(StringComparer.Ordinal))
            Equal(expected[type], actual[type]);
    }

    public static void Equal(GraphCsv expected, GraphCsv actual)
    {
        Assert.True(expected.Columns.SequenceEqual(actual.Columns),
            $"{expected.Type}: schema changed; regenerate expected output");
        var columns = expected.Columns.ToArray();
        var remaining = actual.Rows.ToList();
        var missing = new List<string[]>();
        foreach (var row in expected.Rows)
        {
            var match = remaining.FindIndex(candidate => RowsEqual(columns, row, candidate));
            if (match < 0)
                missing.Add(row);
            else
                remaining.RemoveAt(match);
        }
        Assert.True(missing.Count == 0 && remaining.Count == 0,
            $"{expected.Type}: {missing.Count} missing, {remaining.Count} unexpected rows.\n"
            + $"Missing:\n{Format(missing)}\nUnexpected:\n{Format(remaining)}\n"
            + string.Join("\n", missing.Select(row => DifferingColumns(columns, row, remaining))));
    }

    private static string DifferingColumns(string[] columns, string[] row, List<string[]> unexpected)
    {
        var candidates = unexpected.Where(candidate => candidate[0] == row[0]).ToArray();
        if (candidates.Length != 1)
            return string.Empty;
        var candidate = candidates[0];
        var differing = Enumerable.Range(0, columns.Length)
            .Where(i => !RowsEqual([columns[i]], [row[i]], [candidate[i]]))
            .Select(i => columns[i]);
        return $"{row[0]}: differing columns: {string.Join(", ", differing)}";
    }

    private static string Format(IEnumerable<string[]> rows) =>
        string.Join("\n", rows.Take(5).Select(x => string.Join("\t", x)));

    private static bool RowsEqual(string[] columns, string[] left, string[] right)
    {
        for (var i = 0; i < columns.Length; i++)
        {
            if (left[i] == right[i])
                continue;
            if (!columns[i].EndsWith(":double", StringComparison.Ordinal)
                || !double.TryParse(left[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var a)
                || !double.TryParse(right[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var b)
                || !double.IsFinite(a) || !double.IsFinite(b)
                || Math.Abs(a - b) > Math.Max(1e-12, 1e-9 * Math.Max(Math.Abs(a), Math.Abs(b))))
                return false;
        }
        return true;
    }
}
