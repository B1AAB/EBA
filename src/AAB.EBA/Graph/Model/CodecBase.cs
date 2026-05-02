using System.IO.Compression;

namespace AAB.EBA.Graph.Model;

public class CodecBase<TElement> : IElementCodec
    where TElement : class, IGraphElement
{
    public string DefaultFilename { get; }

    public IElementDescriptor<TElement> Descriptor { get; }

    private readonly bool _serializeCompressed;
    private string? _filename;
    private StreamWriter? _writer;
    private bool _disposed = false;

    public CodecBase(
        IElementDescriptor<TElement> descriptor,
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

            var bufferSize = 1 << 16; // 2^16 = 65536 --> 64KB

            // exclude BOM for UTF-8 (utf identifier) since it breaks Neo4j import. 
            var encoding = new UTF8Encoding(false);

            if (_serializeCompressed)
            {
                _writer = new StreamWriter(
                    new GZipStream(
                        new FileStream(
                            _filename,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            bufferSize: bufferSize,
                            FileOptions.Asynchronous | FileOptions.SequentialScan),
                        CompressionLevel.Fastest,
                        leaveOpen: false),
                    encoding,
                    bufferSize: bufferSize,
                    leaveOpen: false);
            }
            else
            {
                _writer = new StreamWriter(
                    _filename, 
                    append: false,
                    encoding,
                    bufferSize: bufferSize);
            }
        }
        return _writer;
    }

    public async Task WriteCsvAsync(TElement element, string filename)
    {
        await GetStreamWriter(filename).WriteLineAsync(Descriptor.Mapper.ToCsvRow(element));
    }

    public async Task WriteCsvAsync(IEnumerable<TElement> elements, string filename)
    {
        var writer = GetStreamWriter(filename);
        var mapper = Descriptor.Mapper;
        
        // This piece shows better performance in cpu profile compared to using one foreach loop.
        if (elements is TElement[] eArray)
        {
            for (var i = 0; i < eArray.Length; i++)
                mapper.WriteCsvRow(writer, eArray[i]);
        }
        else if (elements is List<TElement> eList)
        {
            for (var i = 0; i < eList.Count; i++)
                mapper.WriteCsvRow(writer, eList[i]);
        }
        else
        {
            foreach (var x in elements)
                mapper.WriteCsvRow(writer, x);
        }
    }

    public virtual string[] GetSchemaConfigs()
    {
        return [];
    }

    public virtual string[] GetSeedingCommands()
    {
        return [];
    }

    public Task WriteCsvAsync(IGraphElement element, string filename)
    {
        return WriteCsvAsync((TElement)element, filename);
    }

    public Task WriteCsvAsync<T>(IEnumerable<T> elements, string filename) where T : IGraphElement
    {
        if (elements is IEnumerable<TElement> typed)
            return WriteCsvAsync(typed, filename);

        return WriteCsvAsync(CastIter(elements), filename);

        static IEnumerable<TElement> CastIter(IEnumerable<T> src)
        {
            foreach (var e in src) 
                yield return (TElement)(object)e!;
        }
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
