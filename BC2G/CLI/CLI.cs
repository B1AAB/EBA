using Spectre.Console;

using System.CommandLine.Help;

using Color = Spectre.Console.Color;
using SecurityException = System.Security.SecurityException;

namespace BC2G.CLI;

internal class Cli
{
    private readonly Parser _parser;
    private readonly RootCommand _rootCmd;
    private readonly Option<string> _workingDirOption;
    private readonly Option<string> _statusFilenameOption;

    public Cli(
        Func<Options, Task> bitcoinTraverseCmdHandlerAsync,
        Func<Options, Task> sampleCmdHandlerAsync,
        Func<Options, Task> bitcoinImportCmdHandlerAsync,
        Action<Exception, InvocationContext> exceptionHandler)
    {
        var defOps = new Options();

        _workingDirOption = new(
            name: "--working-dir",
            description: "The directory where all the data related " +
            "to this execution will be stored.",
            getDefaultValue: () => defOps.WorkingDir);
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

        _statusFilenameOption = new(
            name: "--status-filename",
            description: "A JSON file to store the options used to run BC2G. " +
            "If the file exists, all the options are read from the JSON file " +
            "and the default values used for any missing options, override all " +
            "the options set in the command line.",
            getDefaultValue: () => defOps.StatusFile);

        _rootCmd = new RootCommand(description: "TODO: some description ...");
        _rootCmd.AddGlobalOption(_workingDirOption);
        _rootCmd.AddGlobalOption(_statusFilenameOption);
        // This is required to allow using options without specifying any of the subcommands. 
        _rootCmd.SetHandler(x => { });

        _rootCmd.AddCommand(GetBitcoinCmd(
            defOps,
            bitcoinTraverseCmdHandlerAsync,
            bitcoinImportCmdHandlerAsync,
            sampleCmdHandlerAsync));

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
            .UseHelp(context =>
            {
                context.HelpBuilder.CustomizeLayout(
                    x =>
                    {
                        if (x.ParseResult.Errors.Any())
                            return new List<HelpSectionDelegate>();

                        return HelpBuilder.Default.GetLayout().Prepend(
                       _ => AnsiConsole.Write(
                           new FigletText("BC2G").Color(Color.Purple_1)));
                    });
            })
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
    }

    private Command GetBitcoinCmd(
        Options defOps,
        Func<Options, Task> traverseHandlerAsync,
        Func<Options, Task> importHandlerAsync,
        Func<Options, Task> sampleHandlerAsync)
    {
        var cmd = new Command(
            name: "bitcoin",
            description: "TODO: add some description");
        cmd.AddCommand(GetTraverseCmd(defOps, traverseHandlerAsync));
        cmd.AddCommand(GetImportCmd(defOps, importHandlerAsync));
        cmd.AddCommand(GetSampleCmd(defOps, sampleHandlerAsync));
        return cmd;
    }

    private Command GetTraverseCmd(Options defOps, Func<Options, Task> handlerAsync)
    {
        var fromOption = new Option<int>(
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
            getDefaultValue: () => defOps.Bitcoin.Granularity);

        var skipGraphLoadOption = new Option<bool>(
            name: "--skip-graph-load",
            description: "Running BC2G, Bitcoin-qt, and Neo4j at the same time could put " +
            "a decent amount of compute resource requirement on the system. To alleviate " +
            "it a bit, setting this option would only store the data to be bulk-loaded into " +
            "Neo4j in batches and would not try loading them to Neo4j. After the traverse on " +
            "the chain, these files can be used to load the data into Neo4j.",
            getDefaultValue: () => defOps.Bitcoin.SkipGraphLoad);

        var clientUriOption = new Option<Uri>(
            name: "--client-uri",
            description: "The URI where the Bitcoin client can be reached. The client should " +
            "be started with REST endpoint enabled.",
            getDefaultValue: () => defOps.Bitcoin.ClientUri);

        var cmd = new Command(
            name: "traverse",
            description: "TODO ...")
        {
            fromOption,
            toOption,
            granularityOption,
            skipGraphLoadOption,
            clientUriOption
        };

        cmd.SetHandler(async (options) =>
        {
            await handlerAsync(options);
        },
        new OptionsBinder(
            fromInclusiveOption: fromOption,
            toExclusiveOption: toOption,
            granularityOption: granularityOption,
            skipGraphLoadOption: skipGraphLoadOption,
            bitcoinClientUri: clientUriOption,
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption));

        return cmd;
    }

    private Command GetImportCmd(Options defOps, Func<Options, Task> handlerAsync)
    {
        var cmd = new Command(
            name: "import",
            description: "loads the graph from the CSV files " +
            "created while traversing the blockchain. " +
            "This command should be used when " +
            "--skip-graph-load flag was used.")
        { };

        cmd.SetHandler(async (options) =>
        {
            await handlerAsync(options);
        },
        new OptionsBinder(
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption));

        return cmd;
    }

    private Command GetSampleCmd(Options defOps, Func<Options, Task> handlerAsync)
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
            getDefaultValue: () => defOps.GraphSample.MinNodeCount);

        var maxNodeCountOption = new Option<int>(
            "--max-node-count",
            getDefaultValue: () => defOps.GraphSample.MaxNodeCount);

        var minEdgeCountOption = new Option<int>(
            "--min-edge-count",
            getDefaultValue: () => defOps.GraphSample.MinEdgeCount);

        var maxEdgeCountOption = new Option<int>(
            "--max-edge-count",
            getDefaultValue: () => defOps.GraphSample.MaxEdgeCount);

        var rootNodeSelectProbOption = new Option<double>(
            "--root-node-select-prob",
            description: "The value should be between 0 and 1 (inclusive), " +
            "if the given value is not in this range, it will be replaced " +
            "by the default value.",
            getDefaultValue: () => defOps.GraphSample.RootNodeSelectProb);

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

        cmd.SetHandler(async (options) =>
        {
            await handlerAsync(options);
        },
        new OptionsBinder(
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
}
