using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using BC2G.DAL;

namespace BC2G.CLI
{
    // TODO: CLI and the Options type need to be re-writtent for clarity. 

    internal class CLI
    {
        private readonly RootCommand _rootCmd;
        private readonly Option<string> _workingDirOption = new(
            name: "--working-dir",
            description: "The directory where all the data related " +
            "to this execution will be stored.",
            getDefaultValue: () => new Options().WorkingDir);

        private readonly Option<string> _resumeOption = new(
            name: "--resume",
            description: "The absoloute path to the `status` file " +
            "that can be used to resume a canceled task.");

        private readonly Option<string> _statusFilenameOption = new(
            name: "--status-filename",
            description: "The JSON file to store the execution status.",
            getDefaultValue: () => new Options().StatusFile);

        private readonly Option<double> _httpRequestTimeoutOption = new(
            name: "--http-request-timeout",
            description: "The time in seconds to wait before an http request times out.",
            getDefaultValue: () => new Options().HttpRequestTimeout.TotalSeconds);

        public CLI(
            Func<Options, Task> bitcoinTraverseCmdHandler,
            Func<Options, Task> sampleCmdHandler,
            Func<Options, Task> loadGraphCmdHandler)
        {
            _rootCmd = new RootCommand(description: "TODO: some description ...")
            {
                _resumeOption
            };
            _rootCmd.AddGlobalOption(_workingDirOption);
            _rootCmd.AddGlobalOption(_statusFilenameOption);
            _rootCmd.AddGlobalOption(_httpRequestTimeoutOption);
            // This is required to allow using options without specifying any of the subcommands. 
            _rootCmd.SetHandler(x => { });

            var sampleCmd = GetSampleCmd(sampleCmdHandler);
            _rootCmd.AddCommand(sampleCmd);
            _rootCmd.AddCommand(GetTraverseCmd(bitcoinTraverseCmdHandler));
            _rootCmd.AddCommand(GetLoadGraphCmd(loadGraphCmdHandler));
        }

        public async Task<int> InvokeAsync(string[] args)
        {
            return await _rootCmd.InvokeAsync(args);
        }

        private Command GetSampleCmd(Func<Options, Task> handler)
        {
            // TODO: how can this be fixed?
            // This is an over-kill, we should need to
            // create an instance of this type only to get
            // the default value of a property. 
            var o = new Options();

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
                getDefaultValue: () => o.GraphSampleMinNodeCount);
            var maxNodeCountOption = new Option<int>(
                "--max-node-count",
                getDefaultValue: () => o.GraphSampleMaxNodeCount);

            var minEdgeCountOption = new Option<int>(
                "--min-edge-count",
                getDefaultValue: () => o.GraphSampleMinEdgeCount);
            var maxEdgeCountOption = new Option<int>(
                "--max-edge-count",
                getDefaultValue: () => o.GraphSampleMaxEdgeCount);

            var rootNodeSelectProbOption = new Option<double>(
                "--root-node-select-prob",
                description: "The value should be between 0 and 1 (inclusive), " +
                "if the given value is not in this range, it will be replaced " +
                "by the default value.",

                getDefaultValue: () => o.GraphSampleRootNodeSelectProb);

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
                await handler(options);
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

        private Command GetTraverseCmd(Func<Options, Task> handler)
        {
            var cmd = new Command(
                name: "traverse",
                description: "TODO: add some description");
            cmd.AddCommand(GetBitcoinCmd(handler));

            return cmd;
        }

        private Command GetBitcoinCmd(Func<Options, Task> handler)
        {
            // TODO: it is a bit overkill to get a new instance only to get defaults.
            var optionsForDefaults = new Options();

            var fromOption = new Option<int>(
                name: "--from",
                description: "The inclusive height of the block where the traverse should start.",
                getDefaultValue: () => optionsForDefaults.FromInclusive);

            var toOption = new Option<int>(
                name: "--to",
                description: "The exclusive height of the block where the traverse should end (exclusive).",
                getDefaultValue: () => optionsForDefaults.ToExclusive);

            var granularityOption = new Option<int>(
                name: "--granularity",
                description: "Set the blockchain traversal granularity." +
                "For instance, if set to `10`, it implies processing every 10 blocks in the blockchain.",
                getDefaultValue: () => optionsForDefaults.Granularity);

            var skipGraphLoadOption = new Option<bool>(
                name: "--skip-graph-load",
                description: "Running BC2G, Bitcoin-qt, and Neo4j at the same time could put " +
                "a decent amount of compute resource requirement on the system. To alleviate " +
                "it a bit, setting this option would only store the data to be bulk-loaded into " +
                "Neo4j in batches and would not try loading them to Neo4j. After the traverse on " +
                "the chain, these files can be used to load the data into Neo4j.",
                getDefaultValue: () => optionsForDefaults.SkipLoadGraph);

            var clientUriOption = new Option<Uri>(
                name: "--client-uri",
                description: "The URI where the Bitcoin client can be reached. The client should " +
                "be started with REST endpoint enabled.",
                getDefaultValue: () => optionsForDefaults.BitcoinClientUri);

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

            cmd.SetHandler(async (options) =>
            {
                await handler(options);
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

        private Command GetLoadGraphCmd(Func<Options, Task> handler)
        {
            var o = new Options();

            var cmd = new Command(
                name: "load-graph",
                description: "loads the graph from the CSV files created while traversing the blockchain. This command should be used when --skip-graph-load flag was used.")
            { };

            cmd.SetHandler(async (options) =>
            {
                await handler(options);
            },
            new OptionsBinder(
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption));

            return cmd;
        }
    }
}
