using System.Collections.Concurrent;

namespace BC2G
{
    /// <summary>
    /// This type persists enqueued objects on disk, in 
    /// a non-blocking fashion. It can be used to keep
    /// collections (e.g., List or Dictionary) persisted
    /// on disk.
    /// 
    /// This type is loosly-related to "Memory-mapped files":
    /// <see cref="https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files"/>
    /// </summary>
    public abstract class PersistentObject<T> : IDisposable
    {
        private readonly StreamWriter _stream;
        private readonly BlockingCollection<T> _buffer;

        private bool _disposed = false;

        public PersistentObject(
            string filename,
            CancellationToken cancellationToken,
            string header = "")
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

            _buffer = new();
            var thread = new Thread(() =>
            {
                while (true)
                {
                    T t;
                    try { t = _buffer.Take(cancellationToken); }
                    catch (OperationCanceledException) { break; }

                    if (t != null)
                        _stream.Write(Serialize(t, cancellationToken));
                }
            })
            { IsBackground = false };

            thread.Start();
        }

        public void Enqueue(T obj)
        {
            _buffer.Add(obj);
        }

        public abstract string Serialize(T obj, CancellationToken cancellationToken);

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                if (disposing)
                    _stream.Dispose();

            _disposed = true;
        }
    }
}
