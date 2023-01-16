using SecurityException = System.Security.SecurityException;

namespace BC2G.CommandLineInterface;

internal class CLI
{
    private readonly Parser _parser;
    private readonly RootCommand _rootCmd;
    private readonly Option<string> _workingDirOption;
    private readonly Option<string> _resumeOption;
    private readonly Option<string> _statusFilenameOption;

    public CLI(
        Options options,
        Func<Task> bitcoinTraverseCmdHandler,
        Func<Task> sampleCmdHandler,
        Func<Task> loadGraphCmdHandler,
        Action<Exception, InvocationContext> exceptionHandler)
    {
        _workingDirOption  = new(
            name: "--working-dir",
            description: "The directory where all the data related " +
            "to this execution will be stored.",
            getDefaultValue: () => options.WorkingDir);
        _workingDirOption.AddValidator(x =>
        {
            var result = x.FindResultFor(_workingDirOption);
            if (result is null)
            {
                x.ErrorMessage = "Working directory cannot be null";
            }
            else
            {
                var value = result.GetValueOrDefault<string>();
                if (value is null)
                {
                    x.ErrorMessage = "Working directory cannot be null";
                }
                else
                {
                    string wd;
                    try
                    {
                        wd = Path.GetFullPath(value);
                    }
                    catch (Exception e) when (
                        e is ArgumentException ||
                        e is SecurityException ||
                        e is NotSupportedException ||
                        e is PathTooLongException)
                    {
                        x.ErrorMessage = $"Invalid path `{value}`";
                        return;
                    }

                    Directory.CreateDirectory(wd);
                }
            }
        });

        _resumeOption = new(
            name: "--resume",
            description: "The absoloute path to the `status` file " +
            "that can be used to resume a canceled task.");

        _statusFilenameOption = new(
            name: "--status-filename",
            description: "The JSON file to store the execution status.",
            getDefaultValue: () => options.StatusFile);

        _rootCmd = new RootCommand(description: "TODO: some description ...")
        {
            _resumeOption
        };
        _rootCmd.AddGlobalOption(_workingDirOption);
        _rootCmd.AddGlobalOption(_statusFilenameOption);
        //_rootCmd.AddGlobalOption(_httpRequestTimeoutOption);
        // This is required to allow using options without specifying any of the subcommands. 
        _rootCmd.SetHandler(x => { });

        var sampleCmd = GetSampleCmd(options, sampleCmdHandler);
        _rootCmd.AddCommand(sampleCmd);
        _rootCmd.AddCommand(GetTraverseCmd(options, bitcoinTraverseCmdHandler));
        _rootCmd.AddCommand(GetLoadGraphCmd(options, loadGraphCmdHandler));

        _parser = new CommandLineBuilder(_rootCmd)
            //.UseDefaults() // Do NOT add this since it will cause issues with handling exceptions.
            // System.CommandLine does not let exceptions to propogate from 
            // a handler (a method called from `InvokeAsync`) to the caller
            // of `InvokeAsync`. Which makes its configuration a bit
            // counter-intuitive by requiring using `UseExceptionHandler` as 
            // the following. Hopefully this will get fixed in the future 
            // and this call can be simplified. See this issue: 
            // https://github.com/dotnet/command-line-api/issues/796
            .UseExceptionHandler((e, context) =>
            {
                exceptionHandler(e, context);
            }, 1)
            .UseHelp()
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .CancelOnProcessTermination()
            .Build();
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        return await _parser.InvokeAsync(args);
        //return await _rootCmd.InvokeAsync(args);
    }

    private Command GetSampleCmd(Options options, Func<Task> handler)
    {
        var countOption = new Option<int>(
            name: "--count",
            description: "The number of graphs to sample.")
        { IsRequired = true };
        countOption.AddAlias("-c");

        var hopsOption = new Option<int>(
            name: "--hops",
            description: "The number of hops to reach for sampling.")
        { IsRequired = true };
        hopsOption.AddAlias("-h");

        // TODO: rework this option.
        var modeOption = new Option<GraphSampleMode>(
            name: "--mode",
            description: "Valid values are: " +
            "`A` to generate graph and random edge pairs where the number of random edges equal the number of edges in the graph;" +
            "`B` ",
            isDefault: true,
            parseArgument: x =>
            {
                if (x.Tokens.Count == 0)
                    return default;

                var valid = Enum.TryParse(x.Tokens.Single().Value, out GraphSampleMode value);
                if (!valid)
                    x.ErrorMessage = $"Invalid mode; provided `{value}`, expected `A` or `B`";
                return value;
            });

        var minNodeCountOption = new Option<int>(
            "--min-node-count",
            getDefaultValue: () => options.GraphSample.MinNodeCount);

        var maxNodeCountOption = new Option<int>(
            "--max-node-count",
            getDefaultValue: () => options.GraphSample.MaxNodeCount);

        var minEdgeCountOption = new Option<int>(
            "--min-edge-count",
            getDefaultValue: () => options.GraphSample.MinEdgeCount);

        var maxEdgeCountOption = new Option<int>(
            "--max-edge-count",
            getDefaultValue: () => options.GraphSample.MaxEdgeCount);

        var rootNodeSelectProbOption = new Option<double>(
            "--root-node-select-prob",
            description: "The value should be between 0 and 1 (inclusive), " +
            "if the given value is not in this range, it will be replaced " +
            "by the default value.",
            getDefaultValue: () => options.GraphSample.RootNodeSelectProb);

        var cmd = new Command(
            name: "sample",
            description: "TODO: add some description")
        {
            countOption,
            hopsOption,
            minNodeCountOption,
            maxNodeCountOption,
            minEdgeCountOption,
            maxEdgeCountOption,
            rootNodeSelectProbOption,
            modeOption
        };

        cmd.SetHandler(async (_) =>
        {
            await handler();
        },
        new OptionsBinder(
            options,
            graphSampleCountOption: countOption,
            graphSampleHopOption: hopsOption,
            graphSampleMinNodeCount: minNodeCountOption,
            graphSampleMaxNodeCount: maxNodeCountOption,
            graphSampleMinEdgeCount: minEdgeCountOption,
            graphSampleMaxEdgeCount: maxEdgeCountOption,
            graphSampleModeOption: modeOption,
            graphSampleRootNodeSelectProb: rootNodeSelectProbOption,
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption));

        return cmd;
    }

    private Command GetTraverseCmd(Options options, Func<Task> handler)
    {
        var cmd = new Command(
            name: "traverse",
            description: "TODO: add some description");
        cmd.AddCommand(GetBitcoinCmd(options, handler));

        return cmd;
    }

    private Command GetBitcoinCmd(Options options, Func<Task> handler)
    {
        var fromOption = new Option<int?>(
            name: "--from",
            description: "The inclusive height of the block where the " +
            "traverse should start. If not provided, starts from the " +
            "genesis block (i.e., the first block on the blockchain).");

        var toOption = new Option<int?>(
            name: "--to",
            description: "The exclusive height of the block where the " +
            "traverse should end (exclusive). If not provided, proceeds " +
            "until the last of block on the chain when the process starts.");

        var granularityOption = new Option<int>(
            name: "--granularity",
            description: "Set the blockchain traversal granularity." +
            "For instance, if set to `10`, it implies processing every 10 blocks in the blockchain.",
            getDefaultValue: () => options.Bitcoin.Granularity);

        var skipGraphLoadOption = new Option<bool>(
            name: "--skip-graph-load",
            description: "Running BC2G, Bitcoin-qt, and Neo4j at the same time could put " +
            "a decent amount of compute resource requirement on the system. To alleviate " +
            "it a bit, setting this option would only store the data to be bulk-loaded into " +
            "Neo4j in batches and would not try loading them to Neo4j. After the traverse on " +
            "the chain, these files can be used to load the data into Neo4j.",
            getDefaultValue: () => options.Bitcoin.SkipGraphLoad);

        var clientUriOption = new Option<Uri>(
            name: "--client-uri",
            description: "The URI where the Bitcoin client can be reached. The client should " +
            "be started with REST endpoint enabled.",
            getDefaultValue: () => options.Bitcoin.ClientUri);

        var cmd = new Command(
            name: "bitcoin",
            description: "TODO ...")
        {
            fromOption,
            toOption,
            granularityOption,
            skipGraphLoadOption,
            clientUriOption
        };

        cmd.SetHandler(async (_) =>
        {
            await handler();
        },
        new OptionsBinder(
            options,
            fromInclusiveOption: fromOption,
            toExclusiveOption: toOption,
            granularityOption: granularityOption,
            skipGraphLoadOption: skipGraphLoadOption,
            bitcoinClientUri: clientUriOption,
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption));

        return cmd;
    }

    private Command GetLoadGraphCmd(Options options, Func<Task> handler)
    {
        var cmd = new Command(
            name: "load-graph",
            description: "loads the graph from the CSV files " +
            "created while traversing the blockchain. " +
            "This command should be used when " +
            "--skip-graph-load flag was used.")
        { };

        cmd.SetHandler(async (_) =>
        {
            await handler();
        },
        new OptionsBinder(
            options,
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption));

        return cmd;
    }
}
