namespace BC2G.Graph.Db.Neo4j;

public class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    protected IDriver? Driver { get; }
    protected Options Options { get; }

    /// <summary>
    /// Neo4j docs suggest between 10,000 and 100,000 updates 
    /// per transaction as a good target. 
    /// 
    /// Ref: https://neo4j.com/blog/bulk-data-import-neo4j-3-0/
    /// </summary>
    private const int _maxEntitiesPerBatch = 80000;
    private readonly List<BatchInfo> _batches = new();

    private readonly IMapperFactory _mapperFactory;

    private readonly ILogger<Neo4jDb<T>> _logger;
    private bool _disposed = false;

    public Neo4jDb(Options options, ILogger<Neo4jDb<T>> logger, IMapperFactory mapperFactory)
    {
        Options = options;
        _logger = logger;
        _mapperFactory = mapperFactory;

        if (!Options.Bitcoin.SkipGraphLoad)
        {
            Driver = GraphDatabase.Driver(
                Options.Neo4j.Uri,
                AuthTokens.Basic(Options.Neo4j.User, Options.Neo4j.Password));

            try
            {
                Driver.VerifyConnectivityAsync().Wait();
            }
            catch (AggregateException)
            {
                Dispose(true);
                throw;
            }
        }
    }

    public async void Serialize(T g)
    {
        var batchInfo = GetBatch();

        var edgeTypes = g.GetEdges();
        foreach (var type in edgeTypes)
        {
            var mapper = _mapperFactory.Get(type.Key);

            batchInfo.AddType(type.Key, type.Value.Count);
            var filename = batchInfo.GetFilename(type.Key);

            using var writer = new StreamWriter(filename, append: true);
            if (new FileInfo(filename).Length == 0)
                writer.WriteLine(mapper.GetCsvHeader());

            foreach (var edge in type.Value)
                writer.WriteLine(mapper.GetCsv(edge));

            await JsonSerializer<List<BatchInfo>>.SerializeAsync(
                _batches, Options.Neo4j.BatchesFilename);
        }
    }

    public void Import()
    {

    }

    private BatchInfo GetBatch()
    {
        if (_batches.Count == 0 || _batches[^1].GetTotalCount() >= _maxEntitiesPerBatch)
            _batches.Add(new BatchInfo(Options.Neo4j.ImportDirectory));

        return _batches[^1];
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
            if (disposing) { }
        }

        _disposed = true;
    }
}
