namespace EBA.CLI;

internal class OptionsBinder
{
    /*
    private readonly Option<int>? _fromOption;
    private readonly Option<int?>? _toOption;
    private readonly Option<string?>? _blocksListFileOption;
    private readonly Option<int>? _granularityOption;
    private readonly Option<Uri>? _bitcoinClientUri;
    private readonly Option<int>? _graphSampleCountOption;
    private readonly Option<int>? _graphSampleHopsOption;
    private readonly Option<int>? _graphSampleMinNodeCount;
    private readonly Option<int>? _graphSampleMaxNodeCount;
    private readonly Option<int>? _graphSampleMinEdgeCount;
    private readonly Option<int>? _graphSampleMaxEdgeCount;
    private readonly Option<GraphTraversal>? _graphSampleMethodOption;
    private readonly Option<string>? _graphSampleMethodOptionsOption;
    private readonly Option<double>? _graphSampleRootNodeSelectProb;
    private readonly Option<string>? _workingDirOption;
    private readonly Option<string>? _statusFilenameOption;
    private readonly Option<string>? _batchFilenameOption;
    private readonly Option<int>? _maxBlocksInBufferOption;
    private readonly Option<string>? _txoFilenameOption;
    private readonly Option<bool>? _trackTxoOption;
    private readonly Option<bool>? _skipGraphSerializationOption;
    private readonly Option<int>? _maxEntriesPerBatch;
    private readonly Option<string>? _sortedTxNodeFilenameOption;
    private readonly Option<string>? _sortedScriptNodeFilenameOption;

    public OptionsBinder(
        Option<int>? fromOption = null,
        Option<int?>? toOption = null,
        Option<string?>? blocksListFileOption = null,
        Option<int>? granularityOption = null,
        Option<Uri>? bitcoinClientUri = null,
        Option<int>? graphSampleCountOption = null,
        Option<int>? graphSampleHopOption = null,
        Option<int>? graphSampleMinNodeCount = null,
        Option<int>? graphSampleMaxNodeCount = null,
        Option<int>? graphSampleMinEdgeCount = null,
        Option<int>? graphSampleMaxEdgeCount = null,
        Option<GraphTraversal>? graphSampleMethodOption = null,
        Option<string> graphSampleMethodOptionsOption = null,
        Option<double>? graphSampleRootNodeSelectProb = null,
        Option<string>? workingDirOption = null,
        Option<string>? statusFilenameOption = null,
        Option<string>? batchFilenameOption = null,
        Option<int>? maxBlocksInBufferOption = null,
        Option<string>? txoFilenameOption = null,
        Option<bool>? trackTxoOption = null,
        Option<bool>? skipGraphSerializationOption = null,
        Option<int>? maxEntriesPerBatch = null,
        Option<string>? sortedTxNodeFilenameOption = null,
        Option<string>? sortedScriptNodeFilenameOption = null)
    {
        _fromOption = fromOption;
        _toOption = toOption;
        _blocksListFileOption = blocksListFileOption;
        _granularityOption = granularityOption;
        _bitcoinClientUri = bitcoinClientUri;
        _graphSampleCountOption = graphSampleCountOption;
        _graphSampleHopsOption = graphSampleHopOption;
        _graphSampleMinNodeCount = graphSampleMinNodeCount;
        _graphSampleMaxNodeCount = graphSampleMaxNodeCount;
        _graphSampleMinEdgeCount = graphSampleMinEdgeCount;
        _graphSampleMaxEdgeCount = graphSampleMaxEdgeCount;
        _graphSampleMethodOption = graphSampleMethodOption;
        _graphSampleMethodOptionsOption = graphSampleMethodOptionsOption;
        _graphSampleRootNodeSelectProb = graphSampleRootNodeSelectProb;
        _workingDirOption = workingDirOption;
        _statusFilenameOption = statusFilenameOption;
        _batchFilenameOption = batchFilenameOption;
        _maxBlocksInBufferOption = maxBlocksInBufferOption;
        _txoFilenameOption = txoFilenameOption;
        _trackTxoOption = trackTxoOption;
        _skipGraphSerializationOption = skipGraphSerializationOption;
        _maxEntriesPerBatch = maxEntriesPerBatch;
        _sortedTxNodeFilenameOption = sortedTxNodeFilenameOption;
        _sortedScriptNodeFilenameOption = sortedScriptNodeFilenameOption;
    }*/

    public static Options Build(
        ParseResult c,
        Option<int>? fromOption = null,
        Option<int?>? toOption = null,
        Option<string?>? blocksListFileOption = null,
        Option<int>? granularityOption = null,
        Option<Uri>? bitcoinClientUri = null,
        Option<int>? graphSampleCountOption = null,
        Option<int>? graphSampleHopOption = null,
        Option<int>? graphSampleMinNodeCount = null,
        Option<int>? graphSampleMaxNodeCount = null,
        Option<int>? graphSampleMinEdgeCount = null,
        Option<int>? graphSampleMaxEdgeCount = null,
        Option<GraphTraversal>? graphSampleMethodOption = null,
        Option<string> graphSampleMethodOptionsOption = null,
        Option<double>? graphSampleRootNodeSelectProb = null,
        Option<string>? workingDirOption = null,
        Option<string>? statusFilenameOption = null,
        Option<string>? batchFilenameOption = null,
        Option<int>? maxBlocksInBufferOption = null,
        Option<string>? txoFilenameOption = null,
        Option<bool>? trackTxoOption = null,
        Option<bool>? skipGraphSerializationOption = null,
        Option<int>? maxEntriesPerBatch = null,
        Option<string>? sortedTxNodeFilenameOption = null,
        Option<string>? sortedScriptNodeFilenameOption = null)
    {
        if (statusFilenameOption != null && c.GetResult(statusFilenameOption) is not null)
        {
            var statsFilename = c.GetValue(statusFilenameOption);
            if (statsFilename != null && File.Exists(statsFilename))
            {
                return JsonSerializer<Options>.DeserializeAsync(statsFilename).Result;
            }
        }

        var defs = new Options();

        var wd = GetValue(defs.WorkingDir, workingDirOption, c);

        var bitcoinTraverseOptions = new BitcoinTraverseOptions(defs.Timestamp)
        {
            ClientUri = GetValue(defs.Bitcoin.Traverse.ClientUri, bitcoinClientUri, c),
            From = GetValue(defs.Bitcoin.Traverse.From, fromOption, c),
            To = GetValue(defs.Bitcoin.Traverse.To, toOption, c),
            Granularity = GetValue(defs.Bitcoin.Traverse.Granularity, granularityOption, c),
            BlocksListFile = GetValue(defs.Bitcoin.Traverse.BlocksListFile, blocksListFileOption, c),
            BlocksToProcessListFilename = Path.Join(wd, defs.Bitcoin.Traverse.BlocksToProcessListFilename),
            BlocksFailedToProcessListFilename = Path.Join(wd, defs.Bitcoin.Traverse.BlocksFailedToProcessListFilename),
            MaxBlocksInBuffer = GetValue(defs.Bitcoin.Traverse.MaxBlocksInBuffer, maxBlocksInBufferOption, c),
            TxoFilename = GetValue(defs.Bitcoin.Traverse.TxoFilename, txoFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); }),
            TrackTxo = GetValue(defs.Bitcoin.Traverse.TrackTxo, trackTxoOption, c),
            SkipGraphSerialization = GetValue(defs.Bitcoin.Traverse.SkipGraphSerialization, skipGraphSerializationOption, c)
        };

        var traversalAlgorithm = GetValue(defs.Bitcoin.GraphSample.TraversalAlgorithm, graphSampleMethodOption, c);
        var forestFireOptions = defs.Bitcoin.GraphSample.ForestFireOptions;
        if (traversalAlgorithm == GraphTraversal.FFS)
        {
            if (graphSampleMethodOptionsOption != null && c.GetResult(graphSampleMethodOptionsOption) is not null)
            {
                var jsonValue = c.GetValue(graphSampleMethodOptionsOption);
                if (!string.IsNullOrWhiteSpace(jsonValue))
                    forestFireOptions = JsonSerializer
                        .Deserialize<BitcoinForestFireOptions>(jsonValue)
                        ?? forestFireOptions;
            }
        }

        var gsample = new BitcoinGraphSampleOptions()
        {
            Count = GetValue(defs.Bitcoin.GraphSample.Count, graphSampleCountOption, c),
            Hops = GetValue(defs.Bitcoin.GraphSample.Hops, graphSampleHopOption, c),
            TraversalAlgorithm = traversalAlgorithm,
            ForestFireOptions = forestFireOptions,
            MinNodeCount = GetValue(defs.Bitcoin.GraphSample.MinNodeCount, graphSampleMinNodeCount, c),
            MaxNodeCount = GetValue(defs.Bitcoin.GraphSample.MaxNodeCount, graphSampleMaxNodeCount, c),
            MinEdgeCount = GetValue(defs.Bitcoin.GraphSample.MinEdgeCount, graphSampleMinEdgeCount, c),
            MaxEdgeCount = GetValue(defs.Bitcoin.GraphSample.MaxEdgeCount, graphSampleMaxEdgeCount, c),
            RootNodeSelectProb = GetValue(defs.Bitcoin.GraphSample.RootNodeSelectProb, graphSampleRootNodeSelectProb, c)
        };

        var neo4jOps = new Neo4jOptions()
        {
            BatchesFilename = GetValue(Path.Join(wd, defs.Neo4j.BatchesFilename), batchFilenameOption, c),
            MaxEntitiesPerBatch = GetValue(defs.Neo4j.MaxEntitiesPerBatch, maxEntriesPerBatch, c),
        };

        var bitcoinDedupOps = new BitcoinDedupOptions()
        {
            SortedScriptNodesFilename = GetValue(defs.Bitcoin.Dedup.SortedScriptNodesFilename, sortedScriptNodeFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); }),
            SortedTxNodesFilename = GetValue(defs.Bitcoin.Dedup.SortedTxNodesFilename, sortedTxNodeFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); })
        };

        var bitcoinOps = new BitcoinOptions(defs.Timestamp)
        {
            Traverse = bitcoinTraverseOptions,
            Dedup = bitcoinDedupOps,
            GraphSample = gsample
        };

        var options = new Options()
        {
            WorkingDir = wd,
            StatusFile = GetValue(defs.StatusFile, statusFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); }),
            Logger = new() { LogFilename = Path.Join(wd, Path.GetFileName(defs.Logger.LogFilename)) },
            Bitcoin = bitcoinOps,
            Neo4j = neo4jOps,
        };

        return options;
    }
    /*
    protected override Options GetBoundValue(BindingContext c)
    {
        if (_statusFilenameOption != null && c.ParseResult.HasOption(_statusFilenameOption))
        {
            var statsFilename = c.ParseResult.GetValueForOption(_statusFilenameOption);
            if (statsFilename != null && File.Exists(statsFilename))
            {
                return JsonSerializer<Options>.DeserializeAsync(statsFilename).Result;
            }
        }

        var defs = new Options();

        var wd = GetValue(defs.WorkingDir, _workingDirOption, c);

        var bitcoinTraverseOptions = new BitcoinTraverseOptions(defs.Timestamp)
        {
            ClientUri = GetValue(defs.Bitcoin.Traverse.ClientUri, _bitcoinClientUri, c),
            From = GetValue(defs.Bitcoin.Traverse.From, _fromOption, c),
            To = GetValue(defs.Bitcoin.Traverse.To, _toOption, c),
            Granularity = GetValue(defs.Bitcoin.Traverse.Granularity, _granularityOption, c),
            BlocksListFile = GetValue(defs.Bitcoin.Traverse.BlocksListFile, _blocksListFileOption, c),
            BlocksToProcessListFilename = Path.Join(wd, defs.Bitcoin.Traverse.BlocksToProcessListFilename),
            BlocksFailedToProcessListFilename = Path.Join(wd, defs.Bitcoin.Traverse.BlocksFailedToProcessListFilename),
            MaxBlocksInBuffer = GetValue(defs.Bitcoin.Traverse.MaxBlocksInBuffer, _maxBlocksInBufferOption, c),
            TxoFilename = GetValue(defs.Bitcoin.Traverse.TxoFilename, _txoFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); }),
            TrackTxo = GetValue(defs.Bitcoin.Traverse.TrackTxo, _trackTxoOption, c),
            SkipGraphSerialization = GetValue(defs.Bitcoin.Traverse.SkipGraphSerialization, _skipGraphSerializationOption, c)
        };        

        var traversalAlgorithm = GetValue(defs.Bitcoin.GraphSample.TraversalAlgorithm, _graphSampleMethodOption, c);
        var forestFireOptions = defs.Bitcoin.GraphSample.ForestFireOptions;
        if (traversalAlgorithm == GraphTraversal.FFS)
        {
            if (_graphSampleMethodOptionsOption != null && c.ParseResult.HasOption(_graphSampleMethodOptionsOption))
            {
                var jsonValue = c.ParseResult.GetValueForOption(_graphSampleMethodOptionsOption);
                if (!string.IsNullOrWhiteSpace(jsonValue))
                    forestFireOptions = JsonSerializer
                        .Deserialize<BitcoinForestFireOptions>(jsonValue) 
                        ?? forestFireOptions;
            }
        }

        var gsample = new BitcoinGraphSampleOptions()
        {
            Count = GetValue(defs.Bitcoin.GraphSample.Count, _graphSampleCountOption, c),
            Hops = GetValue(defs.Bitcoin.GraphSample.Hops, _graphSampleHopsOption, c),
            TraversalAlgorithm = traversalAlgorithm,
            ForestFireOptions = forestFireOptions,
            MinNodeCount = GetValue(defs.Bitcoin.GraphSample.MinNodeCount, _graphSampleMinNodeCount, c),
            MaxNodeCount = GetValue(defs.Bitcoin.GraphSample.MaxNodeCount, _graphSampleMaxNodeCount, c),
            MinEdgeCount = GetValue(defs.Bitcoin.GraphSample.MinEdgeCount, _graphSampleMinEdgeCount, c),
            MaxEdgeCount = GetValue(defs.Bitcoin.GraphSample.MaxEdgeCount, _graphSampleMaxEdgeCount, c),
            RootNodeSelectProb = GetValue(defs.Bitcoin.GraphSample.RootNodeSelectProb, _graphSampleRootNodeSelectProb, c)
        };

        var neo4jOps = new Neo4jOptions()
        {
            BatchesFilename = GetValue(Path.Join(wd, defs.Neo4j.BatchesFilename), _batchFilenameOption, c),
            MaxEntitiesPerBatch = GetValue(defs.Neo4j.MaxEntitiesPerBatch, _maxEntriesPerBatch, c),
        };

        var bitcoinDedupOps = new BitcoinDedupOptions()
        {
            SortedScriptNodesFilename = GetValue(defs.Bitcoin.Dedup.SortedScriptNodesFilename, _sortedScriptNodeFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); }),
            SortedTxNodesFilename = GetValue(defs.Bitcoin.Dedup.SortedTxNodesFilename, _sortedTxNodeFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); })
        };

        var bitcoinOps = new BitcoinOptions(defs.Timestamp)
        {
            Traverse = bitcoinTraverseOptions,
            Dedup = bitcoinDedupOps,
            GraphSample = gsample
        };

        var options = new Options()
        {
            WorkingDir = wd,
            StatusFile = GetValue(defs.StatusFile, _statusFilenameOption, c, (x) => { return Path.Join(wd, Path.GetFileName(x)); }),
            Logger = new() { LogFilename = Path.Join(wd, Path.GetFileName(defs.Logger.LogFilename)) },
            Bitcoin = bitcoinOps,
            Neo4j = neo4jOps,
        };

        return options;
    }
    */
    private static T GetValue<T>(T defaultValue, Option<T>? option, ParseResult parseResult, Func<T, T>? composeValue = null)
    {
        var value = defaultValue;

        if (option != null)
        {
            if (parseResult.GetResult(option) != null)
            {
                var givenValue = parseResult.GetValue(option);
                if (givenValue != null)
                    value = givenValue;
            }
        }

        if (value != null && value.Equals(defaultValue) && composeValue != null)
            return composeValue(value);

        return value;
    }
}
