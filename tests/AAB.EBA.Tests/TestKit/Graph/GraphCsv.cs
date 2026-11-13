using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AAB.EBA.Tests.TestKit.Graph;

public sealed class GraphCsv
{
    public string Type { get; }
    public IReadOnlyList<string> Columns { get; }
    public IReadOnlyList<string[]> Rows { get; }

    public GraphCsv(string type, IEnumerable<string> columns, IEnumerable<string[]> rows)
    {
        Type = type;
        Columns = columns.ToArray();
        Rows = rows.Select(x => x.Select(cell => cell.TrimEnd('\r')).ToArray()).ToArray();
        if (Rows.Any(x => x.Length != Columns.Count))
            throw new InvalidDataException($"{type}: row width does not match header.");
    }

    public int Column(string name)
    {
        var index = Array.FindIndex(Columns.ToArray(), x => x == name || x.Split(':')[0] == name);
        return index >= 0 ? index : throw new InvalidDataException($"{Type}: column '{name}' not found.");
    }

    public GraphCsv Where(string column, string value) =>
        new(Type, Columns, Rows.Where(x => x[Column(column)] == value));

    public long Sum(string column) => Rows.Sum(x => long.Parse(x[Column(column)], CultureInfo.InvariantCulture));

    public GraphCsv Project(IEnumerable<string> columns)
    {
        var selected = columns.ToArray();
        var indexes = selected.Select(Column).ToArray();
        return new(Type, selected, Rows.Select(row => indexes.Select(i => row[i]).ToArray()));
    }
}
