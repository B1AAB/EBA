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
    public class PersistentObject<T> : IDisposable
    {
        public bool CanDispose
        {
            get
            {
                return _canCloseStream && (_buffer.Count == 0 || _cancelled);
            }
        }
        private bool _canCloseStream = true;
        private bool _cancelled = false;
        private bool _disposed = false;

        private readonly StreamWriter _stream;
        private readonly BlockingCollection<T> _buffer;

        public PersistentObject(string filename, CancellationToken cT, string header = "")
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
                    T obj;
                    try { obj = _buffer.Take(cT); }
                    catch (OperationCanceledException) { _cancelled = true; break; }

                    if (obj != null)
                    {
                        _canCloseStream = false;
                        _stream.Write(Serialize(obj, cT));
                        PostPersistence(obj);
                        _canCloseStream = true;
                    }
                }
            })
            { IsBackground = false };

            thread.Start();
        }

        public void Enqueue(T obj)
        {
            _buffer.Add(obj);
        }

        public virtual string Serialize(T obj, CancellationToken cT)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            else
                return obj.ToString() ?? throw new ArgumentNullException(nameof(obj));
        }

        public virtual void PostPersistence(T obj) { }

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
                    int graceCount = 3;
                    while (graceCount-- >= 0)
                    {
                        if (_canCloseStream)
                            break;
                        Thread.Sleep(500);
                    }

                    _stream.Flush();
                    _stream.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
