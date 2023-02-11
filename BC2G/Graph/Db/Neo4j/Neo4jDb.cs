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

    /// <summary>
    /// No precedence should be assumed on serializing different types.
    /// </summary>
    public async Task SerializeAsync(T g, CancellationToken ct)
    {
        var edgeTypes = g.GetEdges();
        var graphType = Utilities.TypeToString(g.GetType());
        var batchInfo = await GetBatchAsync(edgeTypes.Keys.Append(graphType).ToList());

        var gMapper = _mapperFactory.GetGraphMapper(graphType);
        batchInfo.AddOrUpdate(graphType, 1);
        gMapper.ToCsv(g, batchInfo.GetFilename(graphType));

        foreach (var type in edgeTypes)
        {
            batchInfo.AddOrUpdate(type.Key, type.Value.Count);
            var eMapper = _mapperFactory.GetEdgeMapper(type.Key);
            eMapper.ToCsv(type.Value, batchInfo.GetFilename(type.Key));
        }

        SerializeBatchesAsync();
    }

    /// <summary>
    /// No precedence should be assumed on serializing different types.
    /// </summary>
    public async Task ImportAsync()
    {
        // TODO: fixme, correct the batchname.
        await ImportAsync("0");
    }
    public async Task ImportAsync(string batchName)
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

        foreach (var type in batch.TypesInfo)
        {
            var mapper = _mapperFactory.GetMapperBase(type.Key);
            await ExecuteQueryAsync(session, mapper, type.Value.Filename);
        }
    }

    private async Task ExecuteQueryAsync(IAsyncSession session, IMapperBase mapper, string filename)
    {
        // Localization, if needed.
        // Neo4j import needs files to be placed in a particular folder 
        // before it can import them.
        var fileLocalized = false;
        var localFilename = filename;

        if (!Utilities.AssertPathEqual(
            Path.GetDirectoryName(filename),
            Options.Neo4j.ImportDirectory))
        {
            localFilename = Path.Join(Options.Neo4j.ImportDirectory, Path.GetFileName(filename));
            File.Copy(filename, localFilename);
            fileLocalized = true;
        }

        var filename4Query = Options.Neo4j.CypherImportPrefix + Path.GetFileName(localFilename);

        var queryResult = await session.ExecuteWriteAsync(async x =>
        {
            IResultCursor cursor = await x.RunAsync(mapper.GetQuery(filename4Query));
            return await cursor.ToListAsync();
        });

        // Delocalization.
        if (fileLocalized)
        {
            File.Delete(localFilename);
        }
    }

    private async Task<BatchInfo> GetBatchAsync(List<string> types)
    {
        if (_batches.Count == 0)
            _batches = await DeserializeBatchesAsync();

        if (_batches.Count == 0 || _batches[^1].GetTotalCount() >= _maxEntitiesPerBatch)
        {
            _batches.Add(new BatchInfo(
                _batches.Count == 0 ? "0" : (_batches.Count + 1).ToString(),
                Options.Neo4j.ImportDirectory, types));
        }

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
