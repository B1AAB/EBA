namespace BC2G.CLI;

internal class OptionsBinder : BinderBase<Options>
{
    private readonly Option<int>? _fromOption;
    private readonly Option<int?>? _toOption;
    private readonly Option<int>? _granularityOption;
    private readonly Option<Uri>? _bitcoinClientUri;
    private readonly Option<int>? _graphSampleCountOption;
    private readonly Option<int>? _graphSampleHopsOption;
    private readonly Option<int>? _graphSampleMinNodeCount;
    private readonly Option<int>? _graphSampleMaxNodeCount;
    private readonly Option<int>? _graphSampleMinEdgeCount;
    private readonly Option<int>? _graphSampleMaxEdgeCount;
    private readonly Option<GraphSampleMode>? _graphSampleModeOption;
    private readonly Option<double>? _graphSampleRootNodeSelectProb;
    private readonly Option<string>? _workingDirOption;
    private readonly Option<string>? _statusFilenameOption;

    public OptionsBinder(
        Option<int>? fromOption = null,
        Option<int?>? toOption = null,
        Option<int>? granularityOption = null,
        Option<bool>? skipGraphLoadOption = null,
        Option<Uri>? bitcoinClientUri = null,
        Option<int>? graphSampleCountOption = null,
        Option<int>? graphSampleHopOption = null,
        Option<int>? graphSampleMinNodeCount = null,
        Option<int>? graphSampleMaxNodeCount = null,
        Option<int>? graphSampleMinEdgeCount = null,
        Option<int>? graphSampleMaxEdgeCount = null,
        Option<GraphSampleMode>? graphSampleModeOption = null,
        Option<double>? graphSampleRootNodeSelectProb = null,
        Option<string>? workingDirOption = null,
        Option<string>? statusFilenameOption = null)
    {
        _fromOption = fromOption;
        _toOption = toOption;
        _granularityOption = granularityOption;
        _bitcoinClientUri = bitcoinClientUri;
        _graphSampleCountOption = graphSampleCountOption;
        _graphSampleHopsOption = graphSampleHopOption;
        _graphSampleMinNodeCount = graphSampleMinNodeCount;
        _graphSampleMaxNodeCount = graphSampleMaxNodeCount;
        _graphSampleMinEdgeCount = graphSampleMinEdgeCount;
        _graphSampleMaxEdgeCount = graphSampleMaxEdgeCount;
        _graphSampleModeOption = graphSampleModeOption;
        _graphSampleRootNodeSelectProb = graphSampleRootNodeSelectProb;
        _workingDirOption = workingDirOption;
        _statusFilenameOption = statusFilenameOption;
    }

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

        var bitcoinOps = new BitcoinOptions()
        {
            ClientUri = GetValue(defs.Bitcoin.ClientUri, _bitcoinClientUri, c),
            From = GetValue(defs.Bitcoin.From, _fromOption, c),
            To = GetValue(defs.Bitcoin.To, _toOption, c),
            Granularity = GetValue(defs.Bitcoin.Granularity, _granularityOption, c),
            BlocksToProcessListFilename = Path.Join(wd, defs.Bitcoin.BlocksToProcessListFilename),
            BlocksFailedToProcessListFilename = Path.Join(wd, defs.Bitcoin.BlocksFailedToProcessListFilename),
            StatsFilename = Path.Join(wd, defs.Bitcoin.StatsFilename)
        };

        var gsample = new GraphSampleOptions()
        {
            Count = GetValue(defs.GraphSample.Count, _graphSampleCountOption, c),
            Hops = GetValue(defs.GraphSample.Hops, _graphSampleHopsOption, c),
            Mode = GetValue(defs.GraphSample.Mode, _graphSampleModeOption, c),
            MinNodeCount = GetValue(defs.GraphSample.MinNodeCount, _graphSampleMinNodeCount, c),
            MaxNodeCount = GetValue(defs.GraphSample.MaxNodeCount, _graphSampleMaxNodeCount, c),
            MinEdgeCount = GetValue(defs.GraphSample.MinEdgeCount, _graphSampleMinEdgeCount, c),
            MaxEdgeCount = GetValue(defs.GraphSample.MaxEdgeCount, _graphSampleMaxEdgeCount, c),
            RootNodeSelectProb = GetValue(defs.GraphSample.RootNodeSelectProb, _graphSampleRootNodeSelectProb, c)
        };

        var neo4jOps = new Neo4jOptions()
        {
            BatchesFilename = Path.Join(wd, defs.Neo4j.BatchesFilename)
        };

        var options = new Options()
        {
            WorkingDir = wd,
            StatusFile = GetValue(defs.StatusFile, _statusFilenameOption, c),
            Bitcoin = bitcoinOps,
            GraphSample = gsample,
            Neo4j = neo4jOps
        };

        return options;
    }

    private static T GetValue<T>(T defaultValue, Option<T>? option, BindingContext context)
    {
        if (option == null)
            return defaultValue;

        var valueGiven = context.ParseResult.FindResultFor(option) != null;

        if (!valueGiven)
            return defaultValue;

        var value = context.ParseResult.GetValueForOption(option);
        if (value == null)
            return defaultValue;

        return value;
    }
}
