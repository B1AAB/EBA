using System.IO.Compression;

namespace BC2G.Graph.Db.Neo4jDb;

public abstract class StrategyBase(bool serializeCompressed) : IDisposable
{
    private string? _filename;
    private StreamWriter? _writer;
    private bool _disposed = false;
    private readonly bool _serializeCompressed = serializeCompressed;

    private StreamWriter GetStreamWriter(string filename)
    {
        if (_writer is null || _filename != filename)
        {
            _filename = filename;

            _writer?.Dispose();

            if (_serializeCompressed)
                _writer = new StreamWriter(new GZipStream(File.Create(_filename), compressionLevel: CompressionLevel.Optimal));
            else
                _writer = new StreamWriter(_filename);//, append: true);

            _writer.AutoFlush = true;

            if (new FileInfo(_filename).Length == 0)
                _writer.WriteLine(GetCsvHeader());

            return _writer;
        }
        else
        {
            return _writer;
        }        
    }

    public async Task ToCsvAsync(IGraphComponent component, string filename)
    {
        await GetStreamWriter(filename).WriteLineAsync(GetCsv(component));
    }

    public async Task ToCsvAsync<T>(IEnumerable<T> components, string filename) where T : IGraphComponent
    {
        await GetStreamWriter(filename).WriteLineAsync(
            string.Join(
                Environment.NewLine,
                from x in components select GetCsv(x)));
    }

    public abstract string GetCsvHeader();

    public abstract string GetCsv(IGraphComponent component);

    public abstract string GetQuery(string filename);

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
