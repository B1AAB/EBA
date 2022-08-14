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

    internal class CommandLineInterface
    {
        private readonly RootCommand _rootCmd;
        private readonly Option<string> _workingDirOption = new(
            name: "--working-dir",
            description: "The directory where all the data related " +
            "to this execution will be stored.");

        private readonly Option<string> _resumeOption = new(
            name: "--resume",
            description: "The absoloute path to the `status` file " +
            "that can be used to resume a canceled task.");

        private readonly Option<string> _statusFilenameOption = new(
            name: "--status-filename",
            description: "The JSON file to store the execution status.",
            isDefault: true,
            parseArgument: x =>
            {
                if (x.Tokens.Count == 0)
                    return "status.json";

                var filePath = x.Tokens.Single().Value;
                return filePath;
            });

        public CommandLineInterface(
            Func<Options, Task> BitcoinTraverseCmdHandler, 
            Func<Options, Task> SampleCmdHandler)
        {
            _rootCmd = new RootCommand(description: "TODO: some description ...")
            {
                _resumeOption
            };
            _rootCmd.AddGlobalOption(_workingDirOption);
            _rootCmd.AddGlobalOption(_statusFilenameOption);
            // This is required to allow using options without specifying any of the subcommands. 
            _rootCmd.SetHandler(x => { });

            var sampleCmd = GetSampleCmd(SampleCmdHandler);
            _rootCmd.AddCommand(sampleCmd);
            _rootCmd.AddCommand(GetTraverseCmd(BitcoinTraverseCmdHandler));
        }

        public async Task<int> InvokeAsync(string[] args)
        {
            return await _rootCmd.InvokeAsync(args);
        }

        private Command GetSampleCmd(Func<Options, Task> handler)
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

            var minNodeCountOption = new Option<int>("--min-node-count");
            var maxNodeCountOption = new Option<int>("--max-node-count");

            var minEdgeCountOption = new Option<int>("--min-edge-count");
            var maxEdgeCountOption = new Option<int>("--max-edge-count");

            var rootNodeSelectProbOption = new Option<double>(
                "--root-node-select-prob",
                description: "The value should be between 0 and 1 (inclusive), " +
                "if the given value is not in this range, it will be replaced " +
                "by the default value.", 
                // TODO: how can this be fixed?
                // This is an over-kill, we should need to
                // get an instance of this type only to get
                // the default value of a property. 
                getDefaultValue: () => new Options().GraphSampleRootNodeSelectProb);

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
            var fromOption = new Option<int>(
                name: "--from",
                description: "The inclusive height of the block where the traverse should start.");

            var toOption = new Option<int>(
                name: "--to",
                description: "The exclusive height of the block where the traverse should end (exclusive).");

            var granularityOption = new Option<int>(
                name: "--granularity",
                description: "Set the blockchain traversal granularity (default is 1)." +
                "For instance, if set to `10`, it implies processing every 10 blocks in the blockchain.");

            var cmd = new Command(
                name: "bitcoin",
                description: "TODO ...")
            {
                fromOption,
                toOption,
                granularityOption
            };

            cmd.SetHandler(async (options) =>
            {
                await handler(options);
            },
            new OptionsBinder(
                fromInclusiveOption: fromOption,
                toExclusiveOption: toOption,
                granularityOption: granularityOption,
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption));

            return cmd;
        }
    }
}
