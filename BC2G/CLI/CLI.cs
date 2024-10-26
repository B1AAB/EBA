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
        Func<Options, Task> addressStatsHandlerAsync,
        Func<Options, Task> importCypherQueriesAsync,
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
            sampleCmdHandlerAsync,
            addressStatsHandlerAsync,
            importCypherQueriesAsync));

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
                            return [];

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
        Options defaultOptions,
        Func<Options, Task> traverseHandlerAsync,
        Func<Options, Task> importHandlerAsync,
        Func<Options, Task> sampleHandlerAsync,
        Func<Options, Task> addressStatsHandlerAsync,
        Func<Options, Task> importCypherQueriesAsync)
    {
        var cmd = new Command(
            name: "bitcoin",
            description: ""); // TODO add some description
        cmd.AddCommand(GetTraverseCmd(defaultOptions, traverseHandlerAsync));
        cmd.AddCommand(GetImportCmd(defaultOptions, importHandlerAsync));
        cmd.AddCommand(GetImportCypherQueriesCmd(defaultOptions, importCypherQueriesAsync));
        cmd.AddCommand(GetSampleCmd(defaultOptions, sampleHandlerAsync));
        cmd.AddCommand(GetAddressStatsCmd(defaultOptions, addressStatsHandlerAsync));
        return cmd;
    }

    private Command GetTraverseCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var fromOption = new Option<int>(
            name: "--from",
            description: "The height of the block where the " +
            "traverse should start. If not provided, starts from the " +
            "genesis block (i.e., the first block on the blockchain).");

        var toOption = new Option<int?>(
            name: "--to",
            description: "The height of the block where the " +
            "traverse should end. If not provided, proceeds " +
            "until the last of block on the chain when the process starts.");

        var granularityOption = new Option<int>(
            name: "--granularity",
            description: "Set the blockchain traversal granularity." +
            "For instance, if set to `10`, it implies processing every 10 blocks in the blockchain.",
            getDefaultValue: () => defaultOptions.Bitcoin.Granularity);

        var clientUriOption = new Option<Uri>(
            name: "--client-uri",
            description: "The URI where the Bitcoin client can be reached. The client should " +
            "be started with REST endpoint enabled.",
            getDefaultValue: () => defaultOptions.Bitcoin.ClientUri);

        var useTxDatabaseOption = new Option<bool>(
            name: "--use-tx-database",
            description: "If set, it uses a postgres database to write and read " +
            "utxo when traversing the blockchain. When using this option, the database size " +
            "can grow big (depending on the number of blocks it traverses), and it can take a " +
            "considerable amount of time to commit transactions to the database. It will be more " +
            "optimal to run the database on a separate persistence media, or ideally use a " +
            "separate database system (e.g., a separate local machine in the network on a database " +
            "service on the cloud). The advantage of this option is that it uses less API requests " +
            "to the Bitcoin agent that can lead to better performance if the database service is " +
            "highly performant; otherwise, the overhead may not lead to a significant performance boost.");

        var addressesFilenameOption = new Option<string>(
            name: "--addresses-filename",
            description: "Sets the filename to persist addresses in each block.");

        var statsFilenameOption = new Option<string>(
            name: "--stats-filename",
            description: "Sets the filename to store statistics collected during the traverse.");

        var maxBufferSizeOption = new Option<int>(
            name: "--max-utxo-buffer-size",
            description: "[Advanced] Sets the maximum number of UTXO to keep in memory. " +
            "This helps reduce the number of API calls to Bitcoin-qt or database queries. " +
            "Buffer size is in linear correlation with memory usage. When max buffer size reaches," +
            "BC2G will implement a strategy the removes (or commits to database) the used Txos, " +
            "keeps a set number of UTXO in the buffer (see --utxo-keep-after-buffer-cleanup), " +
            "and empties the reset of buffer.");

        var utxoKeepAfterCleanupOption = new Option<int>(
            name: "--utxo-keep-after-buffer-cleanup",
            description: "[Advanced] Related to --max-utxo-buffer-size, sets the max number of UTXO to " +
            "keep in memory after buffer size limit has reached and clean up strategy is executed.");

        var cmd = new Command(
            name: "traverse",
            description: "") // TODO: add description
        {
            fromOption,
            toOption,
            granularityOption,
            clientUriOption,
            useTxDatabaseOption,
            statsFilenameOption,
            addressesFilenameOption,
            maxBufferSizeOption,
            utxoKeepAfterCleanupOption,
        };

        cmd.SetHandler(async (options) =>
        {
            await handlerAsync(options);
        },
        new OptionsBinder(
            fromOption: fromOption,
            toOption: toOption,
            granularityOption: granularityOption,
            bitcoinClientUri: clientUriOption,
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption,
            useTxDatabaseOption: useTxDatabaseOption,
            addressesFilenameOption: addressesFilenameOption,
            statsFilenameOption: statsFilenameOption,
            maxBufferSizeOption: maxBufferSizeOption,
            utxoKeepAfterCleanupOption: utxoKeepAfterCleanupOption));

        return cmd;
    }

    private Command GetImportCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var batchFilenameOption = new Option<string>(
            name: "--batch-filename",
            description: "") // TODO: add description
        {
            IsRequired = true
        };

        var cmd = new Command(
            name: "import",
            description: "loads the graph from the CSV files " +
            "created while traversing the blockchain. " +
            "This command should be used when " +
            "--skip-graph-load flag was used.")
        {
            batchFilenameOption
        };

        cmd.SetHandler(async (options) =>
        {
            await handlerAsync(options);
        },
        new OptionsBinder(
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption,
            batchFilenameOption: batchFilenameOption));

        return cmd;
    }

    private Command GetImportCypherQueriesCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var cmd = new Command(
            name: "cypher-queries",
            description: "Writes Neo4j Cypher queries used to import data from batches into a neo4j graph database.");

        cmd.SetHandler(async (options) =>
        {
            await handlerAsync(options);
        },
        new OptionsBinder(
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption));

        return cmd;
    }

    private Command GetSampleCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
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
            getDefaultValue: () => defaultOptions.GraphSample.MinNodeCount);

        var maxNodeCountOption = new Option<int>(
            "--max-node-count",
            getDefaultValue: () => defaultOptions.GraphSample.MaxNodeCount);

        var minEdgeCountOption = new Option<int>(
            "--min-edge-count",
            getDefaultValue: () => defaultOptions.GraphSample.MinEdgeCount);

        var maxEdgeCountOption = new Option<int>(
            "--max-edge-count",
            getDefaultValue: () => defaultOptions.GraphSample.MaxEdgeCount);

        var rootNodeSelectProbOption = new Option<double>(
            "--root-node-select-prob",
            description: "The value should be between 0 and 1 (inclusive), " +
            "if the given value is not in this range, it will be replaced " +
            "by the default value.",
            getDefaultValue: () => defaultOptions.GraphSample.RootNodeSelectProb);

        var cmd = new Command(
            name: "sample",
            description: "") // TODO: add description
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

    private Command GetAddressStatsCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var addressesFilenameOption = new Option<string>(
            name: "--addresses-filename",
            description: "File containing addresses in each block.");

        var statsFilenameOption = new Option<string>(
            name: "--stats-filename",
            description: "File containing the block stats.");

        var cmd = new Command(
            name: "addresses-to-stats",
            description: "Extends the per-block stats with statistics about the " +
            "addresses computed from the file containing addresses in each block.")
        {
            addressesFilenameOption,
            statsFilenameOption
        };


        cmd.SetHandler(async (options) =>
        {
            await handlerAsync(options);
        },
        new OptionsBinder(
            workingDirOption: _workingDirOption,
            statusFilenameOption: _statusFilenameOption,
            addressesFilenameOption: addressesFilenameOption,
            statsFilenameOption: statsFilenameOption));

        return cmd;
    }
}
