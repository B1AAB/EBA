using AAB.EBA.Blockchains.Bitcoin.ChainModel;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db.Neo4jDb;
using AAB.EBA.Graph.Model;
using AAB.EBA.Serializers;
using AAB.EBA.Tests.TestKit.Graph;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// A read-only view of what a Bitcoin traverse left in its working directory:
/// the graph rows written to the batch files, and the blocks that are pending,
/// i.e., the blocks a subsequent traverse will (re)process.
///
/// Rows of the element types that record the height of the block they belong
/// to (e.g., Block nodes and the edges created by a block) are attributed to
/// their block, so the output of a single block can be inspected and compared.
///
/// Full rows are preserved; comparisons use GraphCsvAssert's double tolerance.
/// </summary>
public sealed class TraverseOutput
{
    /// <summary>
    /// For every element type whose rows belong to exactly one block, the
    /// name of the column that holds the height of that block.
    /// Node types that can be shared between blocks (e.g., Tx and Script)
    /// are not attributable to a block, hence they are not listed here.
    /// </summary>
    private static readonly Dictionary<string, string> _blockHeightColumns = new()
    {
        [BlockNode.Kind.ToString()] = nameof(BlockMetadata.Height),
        [B2TEdge.Kind.ToString()] = nameof(B2TEdge.Height),
        [C2TEdge.Kind.ToString()] = nameof(C2TEdge.Height),
        [T2TEdge.KindTransfers.ToString()] = nameof(T2TEdge.Height),
        [T2TEdge.KindFee.ToString()] = nameof(T2TEdge.Height),
        [T2SEdge.Kind.ToString()] = nameof(T2SEdge.CreationHeight),
        [S2TEdge.Kind.ToString()] = nameof(S2TEdge.SpentHeight),
    };

    private static readonly string _blockNodeType = BlockNode.Kind.ToString();

    // element type -> all the rows of that type.
    private readonly Dictionary<string, List<string>> _rowsByType;

    // element type -> block height -> rows of that block;
    // only for the types that are attributable to a block.
    private readonly Dictionary<string, Dictionary<long, List<string>>> _rowsByBlock;

    private readonly TraverseGraphOutput _graph;

    public IReadOnlyDictionary<string, GraphCsv> Tables => _graph.Tables
        .Where(x => x.Key != "Coinbase" && x.Value.Rows.Count > 0)
        .ToDictionary(x => KindName(x.Key), x => x.Value);

    public IReadOnlyDictionary<string, GraphCsv> BlockTables(long height) => _graph.Tables
        .Where(x => _blockHeightColumns.ContainsKey(KindName(x.Key)))
        .ToDictionary(x => KindName(x.Key), x => x.Value.Where(
            _blockHeightColumns[KindName(x.Key)], height.ToString(CultureInfo.InvariantCulture)));

    /// <summary>
    /// The element types that have at least one row in the output.
    /// </summary>
    public IReadOnlyCollection<string> Types => _rowsByType.Keys;

    /// <summary>
    /// Heights of the blocks that have a Block node in the output,
    /// once per Block node (i.e., a block written twice is listed twice).
    /// </summary>
    public IReadOnlyList<long> WrittenBlockHeights { get; }

    /// <summary>
    /// Heights of the blocks a subsequent traverse will process, i.e., the
    /// blocks in the list of blocks to process and in the list of failed
    /// blocks, once per occurrence in those lists.
    /// </summary>
    public IReadOnlyList<long> PendingBlockHeights { get; }

    /// <summary>
    /// Heights in the list of blocks to process, once per occurrence.
    /// </summary>
    public IReadOnlyList<long> ToProcessBlockHeights { get; }

    /// <summary>
    /// Heights in the list of failed blocks, once per occurrence.
    /// </summary>
    public IReadOnlyList<long> FailedBlockHeights { get; }

    private TraverseOutput(
        TraverseGraphOutput graph,
        Dictionary<string, List<string>> rowsByType,
        Dictionary<string, Dictionary<long, List<string>>> rowsByBlock,
        IReadOnlyList<long> toProcessBlockHeights,
        IReadOnlyList<long> failedBlockHeights)
    {
        _graph = graph;
        _rowsByType = rowsByType;
        _rowsByBlock = rowsByBlock;
        ToProcessBlockHeights = toProcessBlockHeights;
        FailedBlockHeights = failedBlockHeights;
        PendingBlockHeights = [.. toProcessBlockHeights, .. failedBlockHeights];

        WrittenBlockHeights = rowsByBlock.TryGetValue(_blockNodeType, out var blockRows)
            ? [.. blockRows.SelectMany(x => Enumerable.Repeat(x.Key, x.Value.Count))]
            : [];
    }

    /// <summary>
    /// Reads the output of a traverse that used the given options. Reading a
    /// file that is not a well-formed (e.g., truncated) compressed file throws.
    /// </summary>
    public static async Task<TraverseOutput> ReadAsync(Options options)
    {
        var rowsByType = new Dictionary<string, List<string>>();
        var rowsByBlock = new Dictionary<string, Dictionary<long, List<string>>>();

        var graph = await TraverseGraphOutput.ReadAsync(options);

        foreach (var table in graph.Tables.Values.Where(x => x.Type != "Coinbase"))
        {
            var type = KindName(table.Type);

            var attributable = _blockHeightColumns.TryGetValue(type, out var column);

            foreach (var fields in table.Rows)
            {
                var row = string.Join(Options.CsvDelimiter, fields);
                GetOrAdd(rowsByType, type).Add(row);

                if (attributable)
                {
                    var height = long.Parse(fields[table.Column(column!)], CultureInfo.InvariantCulture);
                    GetOrAdd(GetOrAdd(rowsByBlock, type), height).Add(row);
                }
            }
        }

        return new TraverseOutput(
            graph,
            rowsByType,
            rowsByBlock,
            ReadHeights(options.Bitcoin.Traverse.BlocksToProcessListFilename),
            ReadHeights(options.Bitcoin.Traverse.BlocksFailedToProcessListFilename));
    }

    /// <summary>
    /// The requested heights that are neither written nor pending.
    /// </summary>
    public IReadOnlyList<long> GetUnaccountedHeights(IEnumerable<long> requested)
    {
        var accounted = WrittenBlockHeights.Concat(PendingBlockHeights).ToHashSet();
        return [.. requested.Where(x => !accounted.Contains(x)).Distinct().Order()];
    }

    /// <summary>
    /// The heights that are accounted for more than once across
    /// written and pending blocks.
    /// </summary>
    public IReadOnlyList<long> GetHeightsAccountedMoreThanOnce()
    {
        return
        [
            .. WrittenBlockHeights
                .Concat(PendingBlockHeights)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .Order()
        ];
    }

    /// <summary>
    /// All the heights accounted for (written or pending), sorted, once each.
    /// </summary>
    public IReadOnlyList<long> GetAccountedHeights()
    {
        return [.. WrittenBlockHeights.Concat(PendingBlockHeights).Distinct().Order()];
    }

    /// <summary>
    /// For every element type not attributable to a block (e.g., Tx and
    /// Script nodes), its row count divided by the number of written blocks.
    /// Empty when no block is written.
    /// </summary>
    public IReadOnlyDictionary<string, int> GetSharedNodeRowCountsPerWrittenBlock()
    {
        if (WrittenBlockHeights.Count == 0)
            return new Dictionary<string, int>();

        return _rowsByType
            .Where(x => !IsAttributableToBlock(x.Key))
            .ToDictionary(x => x.Key, x => x.Value.Count / WrittenBlockHeights.Count);
    }

    /// <summary>
    /// True if every row of the given element type belongs to exactly one
    /// block, hence is included in <see cref="GetBlockRows(long)"/>.
    /// </summary>
    public static bool IsAttributableToBlock(string type)
    {
        return _blockHeightColumns.ContainsKey(type);
    }

    /// <summary>
    /// Returns all the rows of the given element type, sorted.
    /// </summary>
    public IReadOnlyList<string> GetRows(string type)
    {
        return _rowsByType.TryGetValue(type, out var rows)
            ? [.. rows.Order(StringComparer.Ordinal)]
            : [];
    }

    /// <summary>
    /// Returns all the rows in the output, across all element types, as sorted
    /// lines formatted as `{element type}|{row}`. Two outputs contain the same data
    /// (in the comparable form described above) if and only if this
    /// returns the same lines for both.
    /// </summary>
    public IReadOnlyList<string> GetAllRows()
    {
        return
        [
            .. _rowsByType
                .SelectMany(type => type.Value.Select(row => $"{type.Key}|{row}"))
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// Returns all the rows written for the block at the given height, across
    /// all the element types attributable to a block, as sorted lines formatted
    /// as `{element type}|{row}`. Two outputs contain the same data (in the
    /// comparable form described above) for a block if and only if this
    /// returns the same lines for that block.
    /// </summary>
    public IReadOnlyList<string> GetBlockRows(long height)
    {
        return
        [
            .. _rowsByBlock
                .SelectMany(type => type.Value.TryGetValue(height, out var rows)
                    ? rows.Select(row => $"{type.Key}|{row}")
                    : [])
                .Order(StringComparer.Ordinal)
        ];
    }

    private static TValue GetOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        where TValue : new()
    {
        if (!dictionary.TryGetValue(key, out var value))
            dictionary[key] = value = new TValue();
        return value;
    }

    private static long[] ReadHeights(string filename)
    {
        return File.Exists(filename) ? ArraySerializer.Deserialize<long>(filename) : [];
    }

    private static string KindName(string table)
    {
        return table.StartsWith("nodes_", StringComparison.Ordinal) ? table["nodes_".Length..] :
               table.StartsWith("edges_", StringComparison.Ordinal) ? table["edges_".Length..].Replace('_', '-') :
               table;
    }
}
