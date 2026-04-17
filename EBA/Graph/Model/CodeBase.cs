using System.IO.Compression;

namespace EBA.Graph.Model;

public abstract class CodeBase<T> : IElementCodec
    where T : class, IGraphElement
{
    public string DefaultFilename { get; }

    public IElementDescriptor<T> Descriptor { get; }

    private readonly bool _serializeCompressed;
    private string? _filename;
    private StreamWriter? _writer;
    private bool _disposed = false;

    protected CodeBase(
        IElementDescriptor<T> descriptor,
        string defaultFilename,
        bool serializeCompressed)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _serializeCompressed = serializeCompressed;
        DefaultFilename = defaultFilename + (_serializeCompressed ? ".csv.gz" : ".csv");
    }

    public string GetCsvHeader() => Descriptor.Mapper.GetCsvHeader();

    private StreamWriter GetStreamWriter(string filename)
    {
        if (_writer is null || _filename != filename)
        {
            _filename = filename;
            _writer?.Dispose();

            if (_serializeCompressed)
                _writer = new StreamWriter(new GZipStream(File.Create(_filename), CompressionLevel.Optimal));
            else
                _writer = new StreamWriter(_filename);

            _writer.AutoFlush = true;
        }
        return _writer;
    }

    public async Task WriteCsvAsync(T element, string filename)
    {
        await GetStreamWriter(filename).WriteLineAsync(Descriptor.Mapper.ToCsvRow(element));
    }

    public async Task WriteCsvAsync(IEnumerable<T> elements, string filename)
    {
        var writer = GetStreamWriter(filename);
        foreach (var x in elements)
        {
            await writer.WriteLineAsync(Descriptor.Mapper.ToCsvRow(x));
        }
    }

    public abstract string[] GetSchemaConfigs();
    public abstract string[] GetSeedingCommands();

    public Task WriteCsvAsync(IGraphElement element, string filename)
    {
        return WriteCsvAsync((T)element, filename);
    }

    public Task WriteCsvAsync<E>(IEnumerable<E> elements, string filename) where E : IGraphElement
    {
        return WriteCsvAsync(elements.Cast<E>(), filename);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _writer?.Dispose();
            }
            _disposed = true;
        }
    }
}
