using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Xunit;

namespace AAB.EBA.Tests.TestKit.Graph;

public sealed class GraphCsvComparison
{
    private Func<string, string, bool> _include = (_, _) => true;

    public GraphCsvComparison IgnoringColumns(Func<string, string, bool> predicate)
    {
        var previous = _include;
        _include = (type, column) => previous(type, column) && !predicate(type, column);
        return this;
    }

    public GraphCsvComparison OnlyColumns(Func<string, string, bool> predicate)
    {
        var previous = _include;
        _include = (type, column) => previous(type, column) && predicate(type, column);
        return this;
    }

    internal bool Includes(string type, string column) => _include(type, column);
}

public static class GraphCsvAssert
{
    public static void Equal(IReadOnlyDictionary<string, GraphCsv> expected,
        IReadOnlyDictionary<string, GraphCsv> actual, GraphCsvComparison? options = null)
    {
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));
        foreach (var type in expected.Keys.Order(StringComparer.Ordinal))
            Equal(expected[type], actual[type], options);
    }

    public static void Equal(GraphCsv expected, GraphCsv actual, GraphCsvComparison? options = null)
    {
        Assert.True(expected.Columns.SequenceEqual(actual.Columns),
            $"{expected.Type}: schema changed; regenerate expected output");
        var columns = expected.Columns.Where(x => options?.Includes(expected.Type, x) ?? true).ToArray();
        var left = expected.Project(columns);
        var right = actual.Project(columns);
        var remaining = right.Rows.ToList();
        var missing = new List<string[]>();
        foreach (var row in left.Rows)
        {
            var match = remaining.FindIndex(candidate => RowsEqual(columns, row, candidate));
            if (match < 0)
                missing.Add(row);
            else
                remaining.RemoveAt(match);
        }
        Assert.True(missing.Count == 0 && remaining.Count == 0,
            $"{expected.Type}: {missing.Count} missing, {remaining.Count} unexpected rows.\n"
            + $"Missing:\n{Format(missing)}\nUnexpected:\n{Format(remaining)}");
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
