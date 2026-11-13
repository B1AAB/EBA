using AAB.EBA.TestDataGenerator;
using AAB.EBA.Tests.TestKit.Graph;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Xunit;

namespace AAB.EBA.Tests.TestKit.Traverse;

public static class ExpectedGraphStore
{
    public static TraverseGraphOutput Load(long height)
    {
        var directory = Path.Combine(ClientFixture.BitcoinExpectedOutputsDir, "Traverse", height.ToString(CultureInfo.InvariantCulture));
        Assert.True(Directory.Exists(directory), $"Missing expected output for {height}; generate it in Debug with EBA_UPDATE_EXPECTED=1.");
        return new(Directory.GetFiles(directory, "*.tsv").ToDictionary(path => Path.GetFileNameWithoutExtension(path)!,
            path =>
            {
                var lines = File.ReadAllLines(path);
                var type = Path.GetFileNameWithoutExtension(path);
                return new GraphCsv(type, lines[0].TrimEnd('\r').Split('\t'), lines.Skip(1).Select(x => x.TrimEnd('\r').Split('\t')));
            })!);
    }

    public static TraverseGraphOutput Union(params long[] heights)
    {
        var outputs = heights.Select(Load).ToArray();
        var tables = new Dictionary<string, GraphCsv>();
        foreach (var type in outputs.SelectMany(x => x.Tables.Keys).Distinct())
        {
            var parts = outputs.Where(x => x.Tables.ContainsKey(type)).Select(x => x[type]).ToArray();
            Assert.All(parts, x => Assert.Equal(parts[0].Columns, x.Columns));
            tables[type] = new GraphCsv(type, parts[0].Columns,
                type == "Coinbase" ? parts[0].Rows : parts.SelectMany(x => x.Rows));
        }
        return new(tables);
    }

    public static void Compare(long height, TraverseGraphOutput actual, GraphCsvComparison? options = null, string? onlyType = null)
    {
        UpdateIfRequested(height, actual);
        var expected = Load(height);
        if (onlyType is null)
            GraphCsvAssert.Equal(expected.Tables, actual.Tables, options);
        else
            GraphCsvAssert.Equal(expected[onlyType], actual[onlyType], options);
    }

    private static void UpdateIfRequested(long height, TraverseGraphOutput actual)
    {
        if (Environment.GetEnvironmentVariable("EBA_UPDATE_EXPECTED") != "1")
            return;
        Assert.True(SourceBuild.IsDebug, "Expected output can only be updated from a Debug source build.");
        var directory = Path.Combine(AAB.EBA.TestDataGenerator.CLI.GetSolutionRootDirectory(), "tests", "MockData", "Bitcoin", "ExpectedOutputs", "Traverse",
            height.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        foreach (var table in actual.Tables.Values)
        {
            var lines = new[] { string.Join('\t', table.Columns) }
                .Concat(table.Rows.Select(x => string.Join('\t', x)).Order(StringComparer.Ordinal));
            File.WriteAllText(Path.Combine(directory, table.Type + ".tsv"), string.Join('\n', lines) + "\n");
        }
        Assert.Fail($"Expected output updated for {height}.");
    }
}
