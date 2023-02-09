using BC2G.Graph.Db.Bulkload;

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
    private List<BatchInfo> _batches = new();

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

    public async Task SerializeAsync(T g, CancellationToken ct)
    {
        var batchInfo = await GetBatchAsync();

        var edgeTypes = g.GetEdges();
        foreach (var type in edgeTypes)
        {
            var mapper = _mapperFactory.Get(type.Key);

            batchInfo.AddOrUpdate(type.Key, type.Value.Count, Options.Neo4j.ImportDirectory);
            var filename = batchInfo.GetFilename(type.Key);

            using var writer = new StreamWriter(filename, append: true);
            if (new FileInfo(filename).Length == 0)
                writer.WriteLine(mapper.GetCsvHeader());

            foreach (var edge in type.Value)
                writer.WriteLine(mapper.GetCsv(edge));

            SerializeBatchesAsync();
        }
    }

    public void ImportAsync()
    {
        ImportAsync(string.Empty);
    }
    public async void ImportAsync(string batchName)
    {
        if (Driver is null)
            throw new ArgumentNullException(
                nameof(Driver), "A connection to Neo4j is not setup.");

        _batches = await DeserializeBatchesAsync();
        var batch = _batches.Find(x => x.Name == batchName);
        if (batch == default)
            throw new InvalidOperationException(
                $"A batch named {batchName} not found in " +
                $"{Options.Neo4j.BatchesFilename}");

        using var session = Driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Write));

        foreach(var type in batch.TypesInfo)
        {
            var mapper = _mapperFactory.Get(type.Key);
            var bulkLoadResult = session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(mapper.GetQuery(type.Value.Filename));
                return await result.ToListAsync();
            });
            bulkLoadResult.Wait();
        }
    }

    private async Task<BatchInfo> GetBatchAsync()
    {
        if (_batches.Count == 0)
            _batches = await DeserializeBatchesAsync();

        if (_batches.Count == 0)
            _batches.Add(new BatchInfo("0"));
        else if (_batches[^1].GetTotalCount() >= _maxEntitiesPerBatch)
            _batches.Add(new BatchInfo((_batches.Count + 1).ToString()));

        return _batches[^1];
    }
    private async void SerializeBatchesAsync()
    {
        await JsonSerializer<List<BatchInfo>>.SerializeAsync(
            _batches, Options.Neo4j.BatchesFilename);
    }
    private async Task<List<BatchInfo>> DeserializeBatchesAsync()
    {
        return await JsonSerializer<List<BatchInfo>>.DeserializeAsync(
                Options.Neo4j.BatchesFilename);
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
