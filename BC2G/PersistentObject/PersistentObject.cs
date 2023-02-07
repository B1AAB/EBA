namespace BC2G.PersistentObject;

public class PersistentObject<T> : PersistentObjectBase<T>
    where T : notnull
{
    private readonly StreamWriter _stream;

    private bool _disposed = false;

    public PersistentObject(
        string filename, CancellationToken cT, string? header = null) :
        base(cT)
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

    public override Task SerializeAsync(T obj, CancellationToken cT)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        return _stream.WriteAsync(obj.ToString());
    }

    public new void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }
    protected virtual new void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _stream.Flush();
                _stream.Dispose();
            }

            _disposed = true;
        }
    }
}
