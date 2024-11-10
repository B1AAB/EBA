namespace BC2G.PersistentObject;

public class PersistentObject<T> : PersistentObjectBase<T>, IDisposable
    where T : notnull
{
    private readonly StreamWriter _stream;

    private bool _disposed = false;

    public PersistentObject(
        string filename, ILogger<PersistentObject<T>> logger, CancellationToken cT, string? header = null) :
        base(logger, cT)
    {
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentException(
                "Filename cannot be null or empty.");

        if (!File.Exists(filename))
        {
            if (string.IsNullOrEmpty(header))
                File.Create(filename).Dispose();
            else
                File.WriteAllText(filename, header + Environment.NewLine);
        }

        _stream = File.AppendText(filename);
        _stream.AutoFlush = true;
    }

    public override async Task SerializeAsync(T obj, CancellationToken cT)
    {
        ArgumentNullException.ThrowIfNull(obj);

        await _stream.WriteAsync(obj.ToString());
    }

    public override async Task SerializeAsync(IEnumerable<T> objs, CancellationToken cT)
    {
        ArgumentNullException.ThrowIfNull(objs);

        foreach (var obj in objs)
            await _stream.WriteAsync(obj.ToString());
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
