using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace EBA.Graph.Model;

public interface IElementCodec : IDisposable
{
    public string DefaultFilename { get; }
    public string[] GetSchemaConfigs();
    public string[] GetSeedingCommands();
    public abstract string GetCsvHeader();

    public Task WriteCsvAsync(IGraphElement element, string filename);
    public Task WriteCsvAsync<E>(IEnumerable<E> elements, string filename) where E : IGraphElement;

    public static async IAsyncEnumerable<string[]> ReadCsvAsync(
        string filename, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = File.OpenRead(filename);
        using var reader = new StreamReader(
            filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(stream, CompressionMode.Decompress)
            : stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
            yield return line.Split(Options.CsvDelimiter);
    }

    public static IEnumerable<string[]> ReadCsv(string filename)
    {
        using var stream = File.OpenRead(filename);
        using var reader = new StreamReader(
            filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(stream, CompressionMode.Decompress)
            : stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
            yield return line.Split(Options.CsvDelimiter);
    }
}
