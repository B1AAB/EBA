using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BC2G.Model;

internal class PersistentConcurrentQueue : PersistentConcurrentQueue<int>
{
    public PersistentConcurrentQueue(string filename)
        : base(filename)
    { }

    public PersistentConcurrentQueue(string filename, ICollection<int> collection)
        : base(filename, collection)
    { }

    public static new PersistentConcurrentQueue Deserialize(string filename)
    {
        return new PersistentConcurrentQueue(
            filename,
            PersistentConcurrentQueue<int>.Deserialize(filename).ToArray());
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
            var bytes = MemoryMarshal.Cast<T, byte>(this.ToArray<T>());
            using var stream = File.Open(_filename, FileMode.Create);
            stream.Write(bytes);
        }
    }

    public static PersistentConcurrentQueue<T> Deserialize(string filename)
    {
        var items = Array.Empty<T>();

        using (var stream = File.OpenRead(filename))
        {
            int len = checked((int)(stream.Length / Unsafe.SizeOf<T>())), read;
            items = new T[len];
            var bytes = MemoryMarshal.Cast<T, byte>(items);
            while (!bytes.IsEmpty && (read = stream.Read(bytes)) > 0)
                bytes = bytes[read..];
        }

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
