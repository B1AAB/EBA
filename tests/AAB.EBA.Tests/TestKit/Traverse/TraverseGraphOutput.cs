using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Graph.Model;
using AAB.EBA.Tests.TestKit.Graph;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

public sealed class TraverseGraphOutput
{
    public IReadOnlyDictionary<string, GraphCsv> Tables { get; }
    public GraphCsv this[string type] => Tables[type];

    public TraverseGraphOutput(IReadOnlyDictionary<string, GraphCsv> tables) => Tables = tables;

    public static async Task<TraverseGraphOutput> ReadAsync(Options options)
    {
        var tables = Directory.GetFiles(options.WorkingDir, "header_*.csv.gz")
            .ToDictionary(path => Path.GetFileName(path)[7..^7], path =>
                new GraphCsv(Path.GetFileName(path)[7..^7], IElementCodec.ReadCsv(path).Single(), []));
        var rows = tables.Keys.ToDictionary(x => x, _ => new List<string[]>());
        foreach (var batch in await Batch.DeserializeBatchesAsync(options.Neo4j.BatchesFilename))
        {
            foreach (var info in batch.TypesInfo.Values)
            {
                var filename = Path.GetFileName(info.Filename);
                var start = filename.IndexOf("nodes_", StringComparison.Ordinal);
                if (start < 0)
                    start = filename.IndexOf("edges_", StringComparison.Ordinal);
                if (start < 0)
                    throw new InvalidDataException($"Unknown graph filename: {filename}");
                var type = filename[start..^7];
                if (!tables.ContainsKey(type))
                    throw new InvalidDataException($"Missing header for {type}");
                if (File.Exists(info.Filename))
                    rows[type].AddRange(IElementCodec.ReadCsv(info.Filename));
                else if (info.Count != 0)
                    throw new FileNotFoundException("Missing nonempty batch file", info.Filename);
            }
        }
        foreach (var type in tables.Keys.ToArray())
            tables[type] = new GraphCsv(type, tables[type].Columns, rows[type]);
        var coinbase = IElementCodec.ReadCsv(Path.Combine(options.WorkingDir, "Coinbase.csv.gz")).ToArray();
        tables.Add("Coinbase", new GraphCsv("Coinbase", coinbase[0], coinbase.Skip(1)));
        return new(tables);
    }
}
