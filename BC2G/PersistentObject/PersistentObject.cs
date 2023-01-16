namespace BC2G.PersistentObject
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
                return _isFree && (_buffer.Count == 0 || _cancelled);
            }
        }
        private bool _isFree = true;
        private bool _cancelled = false;
        private bool _disposed = false;

        private readonly StreamWriter? _stream;
        private readonly BlockingCollection<T> _buffer;

        //private readonly StreamWriter _nodesStream;
        //private readonly StreamWriter _edgesStream;

        private readonly Task _listner;

        //private readonly Logger _logger;

        public PersistentObject(string filename, /*Logger logger, */CancellationToken cT, string header = "")
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

            _buffer = new();
            //_logger = logger;

            _stream = File.AppendText(filename);
            _stream.AutoFlush = true;

            _listner = Task.Factory.StartNew(
                () =>
                {
                    ListnerAction(
                        (T obj) =>
                        {
                            if (obj != null)
                                _stream.Write(obj.ToString());
                        },
                        cT);
                },
                creationOptions: TaskCreationOptions.LongRunning);

            /*
            _listner = Task.Factory.StartNew(
                delegate () { ListnerAction(cT); },
                creationOptions: TaskCreationOptions.LongRunning);*/

            /*
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

            thread.Start();*/
        }

        // Ideally this and the previous constructors
        // should be merged into a single constructor.
        public PersistentObject(
            //Logger logger,
            //string nodesFilename, 
            //string edgesFilename,
            CancellationToken cT)//,
        /*string nodesHeader = "",
        string edgesHeader = "")*/
        {
            /*
            if (!File.Exists(nodesFilename))
            {
                if (string.IsNullOrEmpty(nodesHeader))
                    File.Create(nodesFilename).Dispose();
                else
                    File.WriteAllText(nodesFilename, nodesHeader + Environment.NewLine);
            }*/

            /*
            if (!File.Exists(edgesFilename))
            {
                if (string.IsNullOrEmpty(edgesHeader))
                    File.Create(edgesFilename).Dispose();
                else
                    File.WriteAllText(edgesFilename, edgesHeader + Environment.NewLine);
            }*/

            //_nodesStream = File.AppendText(nodesFilename);
            //_nodesStream.AutoFlush = true;

            //_edgesStream = File.AppendText(edgesFilename);
            //_edgesStream.AutoFlush = true;

            _buffer = new();
            //_logger = logger;
            _stream = null;

            _listner = Task.Factory.StartNew(
                () =>
                {
                    ListnerAction(
                        (T obj) =>
                        {
                            if (obj != null)
                                Serialize(obj, cT);
                        }, 
                        cT);
                },
                creationOptions: TaskCreationOptions.LongRunning);

            /*
            _listner = Task.Factory.StartNew(
                delegate () { ListnerAction2(cT); },
                creationOptions: TaskCreationOptions.LongRunning);*/

            /*
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
                        Serialize(obj, _nodesStream, _edgesStream, cT);
                        PostPersistence(obj);
                        _canCloseStream = true;
                    }
                }
            })
            { IsBackground = false };

            thread.Start();*/
        }

        private void ListnerAction(Action<T> action, CancellationToken ct)
        {
            while (true)
            {
                T obj;
                try
                {
                    obj = _buffer.Take(ct);
                }
                catch (OperationCanceledException)
                {
                    _cancelled = true;
                    break;
                }

                if (obj == null)
                    continue;

                _isFree = false;
                action(obj);
                PostPersistence(obj);
                _isFree = true;
            }
        }

        /*
        public void ListnerAction(CancellationToken cT)
        {
            while (true)
            {
                T obj;
                try { obj = _buffer.Take(cT); }
                catch (OperationCanceledException) { _cancelled = true; break; }

                if (obj != null)
                {
                    //_logger.Log($"PO1: started processing; buffer contains {_buffer.Count} items.");
                    _isFree = false;
                    _stream.Write(Serialize(obj, cT));
                    //_logger.Log($"PO1: finished serialize, going for post-persistence.");
                    PostPersistence(obj);
                    _isFree = true;
                    //_logger.Log($"PO1: finished.");
                }
            }
        }*/
        /*
        public void ListnerAction2(CancellationToken cT)
        {
            while (true)
            {
                T obj;
                try { obj = _buffer.Take(cT); }
                catch (OperationCanceledException)
                {
                    _cancelled = true; break;
                }
                if (obj != null)
                {
                    //_logger.Log($"PO2: started processing; buffer contains {_buffer.Count} items.");
                    _isFree = false;
                    Serialize(obj, /*_nodesStream, _edgesStream,*/ //cT);
                    //_logger.Log($"PO2: finished serialize, going for post-persistence.");
                    //PostPersistence(obj);
                    //_isFree = true;
                    //_logger.Log($"PO2: finished.");
               // }
           // }
        //}

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
        /*
        public virtual void Serialize(T obj, StreamWriter nodesStream, StreamWriter edgesStream, CancellationToken cT)
        { }*/

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
                        if (_isFree)
                            break;
                        Thread.Sleep(500);
                    }

                    if (_stream is not null)
                    {
                        _stream.Flush();
                        _stream.Dispose();
                    }
                    /*else
                    {
                        /*
                        _nodesStream.Flush();
                        _nodesStream.Dispose();

                        _edgesStream.Flush();
                        _edgesStream.Dispose();*/
                    //}
                }

                _disposed = true;
            }
        }
    }
}
