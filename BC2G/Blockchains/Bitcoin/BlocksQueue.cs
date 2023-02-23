namespace BC2G.Blockchains.Bitcoin;

internal class PersistentConcurrentQueue : PersistentConcurrentQueue<long>
{
    public PersistentConcurrentQueue(string filename)
        : base(filename)
    { }

    public PersistentConcurrentQueue(string filename, ICollection<long> collection)
        : base(filename, collection)
    { }

    public static new PersistentConcurrentQueue Deserialize(string filename)
    {
        return new PersistentConcurrentQueue(
            filename,
            PersistentConcurrentQueue<long>.Deserialize(filename).ToArray());
    }
}

internal class PersistentConcurrentQueue<T> : ConcurrentQueue<T>, IDisposable
    where T : struct
{
    private readonly string _filename;
    private bool _disposed = false;

    private readonly object _lockOnMe = new();

    public PersistentConcurrentQueue(string filename) : base()
    {
        _filename = filename;
    }

    public PersistentConcurrentQueue(string filename, ICollection<T> collection)
        : base(collection)
    {
        _filename = filename;
    }

    public void Serialize()
    {
        lock (_lockOnMe)
        {
            ArraySerializer.Serialize(this.ToArray<T>(), _filename);
        }
    }

    public static PersistentConcurrentQueue<T> Deserialize(string filename)
    {
        var items = ArraySerializer.Deserialize<T>(filename);
        Array.Sort(items);
        return new PersistentConcurrentQueue<T>(filename, items);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Serialize();
            }

            _disposed = true;
        }
    }
}
