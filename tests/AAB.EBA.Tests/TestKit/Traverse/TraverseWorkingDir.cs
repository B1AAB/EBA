using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Db.Neo4jDb;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// A file-level view of the working directory of a traverse, to inspect the
/// artifacts it leaves (status file, batch files, constant files, etc.).
/// For the graph data in those files, see <see cref="TraverseOutput"/>.
/// </summary>
public sealed class TraverseWorkingDir(Options options)
{
    private readonly Options _options = options;

    /// <summary>
    /// The status file parsed as JSON (throws if it is not well-formed).
    /// </summary>
    public JsonDocument StatusFileJson()
    {
        return JsonDocument.Parse(File.ReadAllText(_options.StatusFile));
    }

    /// <summary>
    /// The batches listed in the batches file; empty if there is no such file.
    /// </summary>
    public async Task<IReadOnlyList<Batch>> BatchesAsync()
    {
        // Deserializing a batches file that does not exist creates it.
        return File.Exists(_options.Neo4j.BatchesFilename)
            ? await Batch.DeserializeBatchesAsync(_options.Neo4j.BatchesFilename)
            : [];
    }

    /// <summary>
    /// The files of the batches that exist (a batch has files only for the
    /// types it has rows of).
    /// </summary>
    public async Task<IReadOnlyList<string>> BatchFilesAsync()
    {
        return
        [
            .. (await BatchesAsync())
                .SelectMany(x => x.TypesInfo.Values)
                .Select(x => x.Filename)
                .Where(File.Exists)
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// The element types listed in the batches, across all the batches.
    /// </summary>
    public async Task<IReadOnlyList<string>> BatchTypesAsync()
    {
        return
        [
            .. (await BatchesAsync())
                .SelectMany(x => x.TypesInfo.Keys)
                .Distinct()
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// The batch files that are not well-formed compressed files (e.g.,
    /// truncated), i.e., that cannot be read to their end.
    /// </summary>
    public async Task<IReadOnlyList<string>> UnreadableBatchFilesAsync()
    {
        var unreadable = new List<string>();
        foreach (var file in await BatchFilesAsync())
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var gzip = new GZipStream(stream, CompressionMode.Decompress);
                await gzip.CopyToAsync(Stream.Null);
            }
            catch (InvalidDataException)
            {
                unreadable.Add(Path.GetFileName(file));
            }
        }

        return unreadable;
    }

    /// <summary>
    /// File name -> content, for the files that a traverse writes independent
    /// of the blocks it traverses: the constant (Coinbase*), header (header_*)
    /// and schema (schema.cypher) files. Compressed files are decompressed.
    /// schema.cypher has no paths in it (checked against the mock data),
    /// hence contents are comparable between working directories.
    /// </summary>
    public IReadOnlyDictionary<string, string> ConstantFiles()
    {
        return Directory
            .EnumerateFiles(_options.WorkingDir)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(x =>
                x.StartsWith("Coinbase", StringComparison.Ordinal) ||
                x.StartsWith("header_", StringComparison.Ordinal) ||
                x == "schema.cypher")
            .ToDictionary(x => x, x => ReadContent(Path.Join(_options.WorkingDir, x)));
    }

    /// <summary>
    /// The files in the working directory that cannot be opened exclusively,
    /// i.e., that are still open.
    /// </summary>
    public IReadOnlyList<string> FilesThatCannotBeOpenedExclusively()
    {
        var locked = new List<string>();
        foreach (var file in Directory.EnumerateFiles(_options.WorkingDir, "*", SearchOption.AllDirectories))
        {
            try
            {
                using var _ = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                locked.Add(Path.GetFileName(file));
            }
        }

        return locked;
    }

    /// <summary>
    /// The kinds of the files in the working directory, where the kind of a
    /// file is its name without the timestamp and optional batch number prefix.
    /// </summary>
    public IReadOnlyList<string> FileKinds()
    {
        return
        [
            .. Directory
                .EnumerateFiles(_options.WorkingDir)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Select(x => Regex.Replace(x, @"^[0-9]+(?:_[0-9]+)?(?=_)", ""))
                .Distinct()
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// File name -> SHA-256 of its content, for every file in the working directory.
    /// </summary>
    public IReadOnlyDictionary<string, string> Snapshot()
    {
        return Directory
            .EnumerateFiles(_options.WorkingDir)
            .ToDictionary(
                x => Path.GetFileName(x),
                x => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x))));
    }

    private static string ReadContent(string filename)
    {
        using var stream = File.OpenRead(filename);
        using var reader = filename.EndsWith(".gz", StringComparison.Ordinal)
            ? new StreamReader(new GZipStream(stream, CompressionMode.Decompress))
            : new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
