using BC2G.Utilities;

namespace BC2G.PersistentObject;

public class PersistentObject<T> : PersistentObjectBase<T>, IDisposable
    where T : notnull
{
    private StreamWriter _stream;
    private readonly string _baseFilename;
    private readonly string? _header;
    private readonly int _maxObjsPerStream;

    private bool _disposed = false;

    // TODO: it is better to have this counter in the base class, 
    // but since the design allows calling the "SerializeAsync" directly
    // from public, then the counter in the base class may not be 
    // accurate as the method updating it (e.g., ListnerActionAsync) may never execute. 
    private int _objsSerializedInCurrentBatch = 0;

    public PersistentObject(
        string filename,
        int maxObjectsPerFile,
        ILogger<PersistentObject<T>> logger,
        CancellationToken cT,
        string? header = null) :
        base(logger, cT)
    {
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentException(
                "Filename cannot be null or empty.");
        _baseFilename = filename;
        _header = header;
        _maxObjsPerStream = maxObjectsPerFile;

         CreateStream();
    }

    private void CreateStream()
    {
        var filename = Path.Join(
            Path.GetDirectoryName(_baseFilename),
            $"{Helpers.GetUnixTimeSeconds()}_{Path.GetFileName(_baseFilename)}");

        if (!File.Exists(filename))
        {
            if (string.IsNullOrEmpty(_header))
                File.Create(filename).Dispose();
            else
                File.WriteAllText(filename, _header + Environment.NewLine);
        }

        _stream?.Dispose();
        _stream = File.AppendText(filename);
        _stream.AutoFlush = true;
    }

    public override async Task SerializeAsync(T obj, CancellationToken cT)
    {
        if (_objsSerializedInCurrentBatch >= _maxObjsPerStream)
        {
            CreateStream();
            _objsSerializedInCurrentBatch = 0;
        }

        await _stream.WriteLineAsync(obj.ToString());
        Interlocked.Increment(ref _objsSerializedInCurrentBatch);
    }

    public override async Task SerializeAsync(IEnumerable<T> objs, CancellationToken cT)
    {
        if (_objsSerializedInCurrentBatch >= _maxObjsPerStream)
        {
            CreateStream();
            _objsSerializedInCurrentBatch = 0;
        }

        foreach (var obj in objs)
        {
            await _stream.WriteLineAsync(obj.ToString());
            Interlocked.Increment(ref _objsSerializedInCurrentBatch);
        }
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual new void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _stream.Flush();
                _stream.Dispose();
            }

            _disposed = true;
        }
    }
}
