namespace BC2G.PersistentObject;

/// TODO 1
/// If an exception is occurred in this class, the exception will not
/// be propogated to the caller, because the caller does not wait for
/// this taks to finish, which is correct, because this task only exits
/// when the application exists. So, it will never finish until the
/// program exits.
/// 
/// TODO 2
/// This needs a max buffer size, and a method or a mechanism to 
/// wait for buffer to empty before enqueueing more objects.

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
        _buffer = [];
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

                // TODO: This is temporary only to get details of errors to reproduce/debug them.
                _logger.LogDebug("Exception details: {e}", e);

                // TODO: re-throwing exception here has no impact.
                // fixing it requires a bit of reengineering how this method is used.
                // The exception does not propogate because the caller does not 
                // wait for this method to finish, which is by-design as this
                // method only exits when the application is exiting.
                // The following is a _temp_ work-around.
                //
                //
                // One of the examples of this exception is when the file size is `397,633,060,864 bytes`
                // when you this method will throw the following exception:
                //
                //      Exception occurred persisting an instance of type
                //      `BC2G.Blockchains.Bitcoin.Graph.BlockGraph`.
                //      The requested operation could not be completed due
                //      to a file system limitation : 'bitcoin_txo.tsv'
                Environment.Exit(1);
            }
            _isFree = true;
        }
    }

    public void Enqueue(T obj)
    {
        _buffer.Add(obj);
    }


    // TODO: the following two methods should be changed to "protected",
    // then all the derived classes implementing these should also make 
    // their methods "protected" which results in making only the 
    // "Enqueue" method public. 
    // Currently, some instances of derived classes, for instance, 
    // _pGraphStats and _pBlockAddressess are using the "SerializeAsync"
    // methods directly that does not use the above buffer. 
    // Think of improving this design and make it clear which 
    // method should be used and how. 

    public abstract Task SerializeAsync(T obj, CancellationToken cT);
    public abstract Task SerializeAsync(IEnumerable<T> objs, CancellationToken cT);

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
