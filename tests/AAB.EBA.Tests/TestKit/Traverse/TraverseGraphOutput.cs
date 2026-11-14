using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Graph.Bitcoin.Descriptors;
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
        var headers = Directory.Exists(options.WorkingDir)
            ? Directory.GetFiles(options.WorkingDir, "header_*.csv.gz") : [];
        var tables = headers.ToDictionary(HeaderType, path =>
            new GraphCsv(HeaderType(path), IElementCodec.ReadCsv(path).Single(), []));
        if (headers.Length == 0)
        {
            using var strategies = new BitcoinStrategyFactory(options);
            foreach (var (kind, codec) in strategies.NodeStrategies)
            {
                var type = codec.DefaultFilename[..^".csv.gz".Length];
                tables.Add(type, new GraphCsv(type, codec.GetCsvHeader().Split(Options.CsvDelimiter), []));
            }
            foreach (var (kind, codec) in strategies.EdgeStrategies)
            {
                var type = codec.DefaultFilename[..^".csv.gz".Length];
                tables.Add(type, new GraphCsv(type, codec.GetCsvHeader().Split(Options.CsvDelimiter), []));
            }
        }
        var rows = tables.Keys.ToDictionary(x => x, _ => new List<string[]>());
        var batches = File.Exists(options.Neo4j.BatchesFilename)
            ? await Batch.DeserializeBatchesAsync(options.Neo4j.BatchesFilename) : [];
        foreach (var batch in batches)
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
                if (!tables.ContainsKey(type) && !File.Exists(info.Filename) && info.Count == 0)
                    continue;
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
        var coinbaseFilename = Path.Combine(options.WorkingDir, "Coinbase.csv.gz");
        if (File.Exists(coinbaseFilename))
        {
            var coinbase = IElementCodec.ReadCsv(coinbaseFilename).ToArray();
            tables.Add("Coinbase", new GraphCsv("Coinbase", coinbase[0], coinbase.Skip(1)));
        }
        return new(tables);
    }

    private static string HeaderType(string path) =>
        Path.GetFileName(path).Substring("header_".Length, Path.GetFileName(path).Length - "header_".Length - ".csv.gz".Length);
}
