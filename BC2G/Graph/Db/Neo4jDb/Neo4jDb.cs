namespace BC2G.Graph.Db.Neo4jDb;

public static class Neo4jDb
{
    public const string csvDelimiter = "\t";
}

public abstract class Neo4jDb<T> : IGraphDb<T> where T : GraphBase
{
    protected Options Options { get; }
    protected IStrategyFactory StrategyFactory { get; }

    /// <summary>
    /// Neo4j docs suggest between 10,000 and 100,000 updates 
    /// per transaction as a good target. 
    /// 
    /// Ref: https://neo4j.com/blog/bulk-data-import-neo4j-3-0/
    /// </summary>
    private const int _maxEntitiesPerBatch = 80000;
    private List<Batch> _batches = new();

    private readonly ILogger<Neo4jDb<T>> _logger;
    private bool _disposed = false;

    public Neo4jDb(Options options, ILogger<Neo4jDb<T>> logger, IStrategyFactory strategyFactory)
    {
        Options = options;
        _logger = logger;
        StrategyFactory = strategyFactory;
    }

    public abstract Task SerializeAsync(T g, CancellationToken ct);

    /// <summary>
    /// No precedence should be assumed on serializing different types.
    /// </summary>
    public virtual async Task ImportAsync(CancellationToken ct, string batchName = "", List<GraphComponentType>? importOrder = null)
    {
        using var driver = await GetDriver(Options.Neo4j);

        _batches = await DeserializeBatchesAsync();
        IEnumerable<Batch> batches;

        if (string.IsNullOrEmpty(batchName))
        {
            batches = _batches;
            _logger.LogInformation("No batch name is given, processing all the batches in the given batch file.");
        }
        else
        {
            var batch = _batches.Find(x => x.Name == batchName);
            if (batch == default)
                throw new InvalidOperationException(
                    $"A batch named {batchName} not found in " +
                    $"{Options.Neo4j.BatchesFilename}");
            batches = new List<Batch>() { batch };
            _logger.LogInformation("Given batch name is {batchName}.", batchName);
        }

        if (!batches.Any())
        {
            _logger.LogInformation("No batch found in {f}.", Options.Neo4j.BatchesFilename);
            return;
        }

        _logger.LogInformation(
            "Processing {n:n0} batch(es) found in {f}.",
            batches.Count(), Options.Neo4j.BatchesFilename);

        var c = 0;
        var importRuntime = TimeSpan.Zero;
        var stopwatch = new Stopwatch();

        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            _logger.LogInformation("Processing batch {b} {c}.", batch.Name, $"({++c:n0}/{batches.Count():n0})");

            var typesInfo = batch.TypesInfo;
            var typesKeys = importOrder ?? typesInfo.Keys;
            foreach (var typeKey in typesKeys)
            {
                stopwatch.Restart();
                if (!typesInfo.ContainsKey(typeKey))
                {
                    _logger.LogInformation(
                        "Skipping type {t} since the batch {b} does not contain it.",
                        typeKey, batch.Name);
                    continue;
                }

                _logger.LogInformation("Importing type {t}.", typeKey);
                var strategy = StrategyFactory.GetStrategy(typeKey);
                await ExecuteLoadQueryAsync(driver, strategy, typesInfo[typeKey].Filename);

                stopwatch.Stop();
                importRuntime += stopwatch.Elapsed;
                _logger.LogInformation(
                    "Importing type {t} finished in {et} seconds.", 
                    typeKey, Utilities.GetEtInSeconds(stopwatch.Elapsed));
            }
        }

        _logger.LogInformation("Successfully finished import in {et}.", importRuntime);
    }

    public async Task<bool> TrySampleAsync()
    {
        var driver = await GetDriver(Options.Neo4j);

        var sampledGraphsCounter = 0;
        var attempts = 0;
        var baseOutputDir = Path.Join(Options.WorkingDir, $"sampled_graphs_{Utilities.GetTimestamp()}");

        while (
            sampledGraphsCounter < Options.GraphSample.Count
            && ++attempts <= Options.GraphSample.MaxAttempts)
        {
            _logger.LogInformation(
                "Sampling {n} graphs; remaining {r}; attempt {a}/{m}.",
                Options.GraphSample.Count,
                Options.GraphSample.Count - sampledGraphsCounter,
                attempts, Options.GraphSample.MaxAttempts);

            var rndRootNodes = await GetRandomNodes(
                driver,
                Options.GraphSample.Count - sampledGraphsCounter,
                Options.GraphSample.RootNodeSelectProb);

            foreach (var rootNode in rndRootNodes)
            {
                var baseDir = Path.Join(baseOutputDir, sampledGraphsCounter.ToString());
                if (await TrySampleNeighborsAsync(driver, rootNode, baseDir))
                {
                    sampledGraphsCounter++;
                    _logger.LogInformation(
                        "Finished writting sampled graph {n}/{t} features to {b}.",
                        sampledGraphsCounter,
                        Options.GraphSample.Count,
                        baseDir);
                }
            }
        }

        if (attempts > Options.GraphSample.MaxAttempts)
        {
            _logger.LogError(
                "Failed creating {g} {g_msg} after {a} {a_msg}; created {c} {c_msg}. " +
                "You may retry, and if the error persists, try adjusting the values of " +
                "{minN}={minNV}, {maxN}={maxNV}, {minE}={minEV}, and {maxE}={maxEV}.",
                Options.GraphSample.Count,
                Options.GraphSample.Count > 1 ? "graphs" : "graph",
                attempts - 1,
                attempts > 1 ? "attempts" : "attempt",
                sampledGraphsCounter,
                sampledGraphsCounter > 1 ? "graphs" : "graph",
                nameof(Options.GraphSample.MinNodeCount), Options.GraphSample.MinNodeCount,
                nameof(Options.GraphSample.MaxNodeCount), Options.GraphSample.MaxNodeCount,
                nameof(Options.GraphSample.MinEdgeCount), Options.GraphSample.MinEdgeCount,
                nameof(Options.GraphSample.MaxEdgeCount), Options.GraphSample.MaxEdgeCount);
            return false;
        }
        else
        {
            return true;
        }
    }

    public virtual async Task<IDriver> GetDriver(Neo4jOptions options)
    {
        var driver = GraphDatabase.Driver(
            options.Uri,
            AuthTokens.Basic(options.User, options.Password));

        try
        {
            await driver.VerifyConnectivityAsync();
        }
        catch (AggregateException)
        {
            throw;
        }

        return driver;
    }

    private async Task ExecuteLoadQueryAsync(IDriver driver, StrategyBase strategy, string filename)
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
            File.Copy(filename, localFilename, true);
            fileLocalized = true;
        }

        var filename4Query = Options.Neo4j.CypherImportPrefix + Path.GetFileName(localFilename);

        using var session = driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));
        try
        {
            var queryResult = await session.ExecuteWriteAsync(async x =>
            {
                IResultCursor cursor = await x.RunAsync(strategy.GetQuery(filename4Query));
                return await cursor.ToListAsync();
            });
        }
        catch(Exception e)
        {
            _logger.LogCritical(
                "The folloiwng exceptions occurred executing a Neo4j query. {e}",
                string.Join("; ", e.InnerException?.Message));
            throw;
        }

        // Delocalization.
        if (fileLocalized)
        {
            File.Delete(localFilename);
        }
    }

    protected async Task<Batch> GetBatchAsync(List<GraphComponentType> types)
    {
        if (_batches.Count == 0)
            _batches = await DeserializeBatchesAsync();

        if (_batches.Count == 0 || _batches[^1].GetMaxCount() >= _maxEntitiesPerBatch)
            _batches.Add(new Batch(_batches.Count.ToString(), Options.WorkingDir, types));

        return _batches[^1];
    }

    protected void SerializeBatches()
    {
        JsonSerializer<List<Batch>>.Serialize(
            _batches, Options.Neo4j.BatchesFilename);
    }

    protected async Task SerializeBatchesAsync()
    {
        await JsonSerializer<List<Batch>>.SerializeAsync(
            _batches, Options.Neo4j.BatchesFilename);
    }

    private async Task<List<Batch>> DeserializeBatchesAsync()
    {
        return await JsonSerializer<List<Batch>>.DeserializeAsync(
            Options.Neo4j.BatchesFilename);
    }

    public static bool CanUseGraph(
        GraphBase g,
        int minNodeCount = 3, int maxNodeCount = 200,
        int minEdgeCount = 3, int maxEdgeCount = 200,
        double tolerance = 0.5)
    {
        // TODO: implement checks on the graph; e.g., graph size, or if it was already defined.

        // TODO: very big graphs cause various issues
        // with Tensorflow when training, such as out-of-memory
        // (hence radically slow process), or even trying to
        // multiply matrixes of very large size 2**32 or even
        // larger. There should be much better workarounds at
        // Tensorflow level, but for now, we limit the size of graphs.
        if (g.NodeCount <= minNodeCount - (minNodeCount * tolerance) ||
            g.NodeCount >= maxNodeCount + (maxNodeCount * tolerance) ||
            g.EdgeCount <= minEdgeCount - (minEdgeCount * tolerance) ||
            g.EdgeCount >= maxEdgeCount + (maxEdgeCount * tolerance))
            return false;

        return true;
    }


    // TODO: Make the following methods more generic, e.g., replace ScriptNode with INode
    public abstract Task<List<ScriptNode>> GetRandomNodes(
        IDriver driver, int nodesCount, double rootNodesSelectProb = 0.1);

    public abstract Task<GraphBase> GetNeighborsAsync(
        IDriver driver, string rootScriptAddress, int maxHops);

    public abstract Task<GraphBase> GetRandomEdges(
        IDriver driver, int edgeCount, double edgeSelectProb = 0.2);

    public abstract Task<bool> TrySampleNeighborsAsync(
        IDriver driver, ScriptNode rootNode, string baseOutputDir);

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
                StrategyFactory.Dispose();
                SerializeBatches();
            }
        }

        _disposed = true;
    }
}
