namespace BC2G.PersistentObject;

/// TODO
/// If an exception is occurred in this class, the exception will not
/// be propogated to the caller, because the caller does not wait for
/// this taks to finish, which is correct, because this task only exits
/// when the application exists. So, it will never finish until the
/// program exits.

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
    private bool _isFree = true;
    private bool _cancelled = false;
    private bool _disposed = false;

    private readonly BlockingCollection<T> _buffer;
    private readonly ILogger<PersistentObjectBase<T>> _logger;

    public PersistentObjectBase(ILogger<PersistentObjectBase<T>> logger, CancellationToken cT)
    {
        _buffer = new();
        _logger = logger;

        var listner = Task.Factory
            .StartNew(
                async () => await ListnerActionAsync(cT),
                creationOptions: TaskCreationOptions.LongRunning)
            .ContinueWith(
                task => { },
                continuationOptions: TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ListnerActionAsync(CancellationToken cT)
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
            try
            {
                await SerializeAsync(obj, cT);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception occurred persisting an instance of type `{o}`. {e}", 
                    obj, e.Message);
                // TODO: re-throwing exception here has no impact.
                // fixing it requires a bit of reengineering how this method is used.
                // The exception does not propogate because the caller does not 
                // wait for this method to finish, which is by-design as this
                // method only exits when the application is exiting.
                // The following is a _temp_ work-around.
                Environment.Exit(1);
            }
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
                while ((_cancelled && graceCount-- >= 0) || true)
                {
                    if (_isFree && (_buffer.Count == 0 || _cancelled))
                        break;
                    Thread.Sleep(500);
                }
            }

            _disposed = true;
        }
    }
}
