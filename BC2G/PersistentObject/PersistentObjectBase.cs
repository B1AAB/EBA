namespace BC2G.PersistentObject;

/// <summary>
/// Persists enqueued objects on disk in 
/// a non-blocking fashion.
/// 
/// The concepts implemented here are loosly-related to "Memory-mapped files":
/// <see cref="https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files"/>
/// </summary>
public abstract class PersistentObjectBase<T> : IDisposable
    where T : notnull
{
    public bool CanDispose
    {
        get { return _isFree && (_buffer.Count == 0 || _cancelled); }
    }

    private bool _isFree = true;
    private bool _cancelled = false;
    private bool _disposed = false;

    private readonly BlockingCollection<T> _buffer;

    public PersistentObjectBase(CancellationToken cT)
    {
        _buffer = new();

        var listner = Task.Factory
            .StartNew(
                () => ListnerAction(cT),
                creationOptions: TaskCreationOptions.LongRunning)
            .ContinueWith(
                task => { },
                continuationOptions: TaskContinuationOptions.OnlyOnFaulted);
    }

    private async void ListnerAction(CancellationToken cT)
    {
        while (true)
        {
            T obj;
            try
            {
                obj = _buffer.Take(cT);
            }
            catch (OperationCanceledException)
            {
                _cancelled = true;
                break;
            }

            if (obj == null)
                continue;

            _isFree = false;
            await SerializeAsync(obj, cT);
            _isFree = true;
        }
    }

    public void Enqueue(T obj)
    {
        _buffer.Add(obj);
    }

    public abstract Task SerializeAsync(T obj, CancellationToken cT);

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
                int graceCount = 3;
                while (graceCount-- >= 0)
                {
                    if (_isFree)
                        break;
                    Thread.Sleep(500);
                }
            }

            _disposed = true;
        }
    }
}
