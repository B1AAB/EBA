using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using BC2G.DAL;

namespace BC2G.CLI
{
    internal class CommandLineInterface
    {
        private readonly RootCommand _rootCmd;
        private readonly Option<DirectoryInfo?> _workingDirOption = new(
            name: "--working-dir",
            description: "The directory where all the data related " +
            "to this execution will be stored.");

        private readonly Option<FileInfo?> _resumeOption = new(
            name: "--resume",
            description: "The absoloute path to the `status` file " +
            "that can be used to resume a canceled task.");

        private readonly Option<FileInfo?> statusFilenameOption = new(
            name: "--status-filename",
            description: "The JSON file to store the execution status.",
            isDefault: true,
            parseArgument: x =>
            {
                if (x.Tokens.Count == 0)
                    return new FileInfo("abc.json"); // TODO: fixme. 

                var filePath = x.Tokens.Single().Value;
                return new FileInfo(filePath);
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
            _rootCmd.AddGlobalOption(statusFilenameOption);

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
                description: "The number of graphs to sample.");

            // TODO: rework this option.
            var modeOption = new Option<GraphSampleMode>(
                name: "--mode",
                description: "Valid values are: " +
                "`A` to generate graph and random edge pairs where the number of random edges equal the number of edges in the graph;" +
                "`B` ",
                isDefault: true,
                parseArgument: x =>
                {
                    var valid = Enum.TryParse(x.Tokens.Single().Value, out GraphSampleMode value);
                    if (!valid)
                        x.ErrorMessage = $"Invalid mode; provided `{value}`, expected `A` or `B`";
                    return value;
                });

            var cmd = new Command(
                name: "sample",
                description: "TODO: add some description")
            {
                countOption,
                //outputDirOption,
                modeOption
            };

            cmd.SetHandler(async (workingDir, options) =>
            {
                options.OutputDirectory = workingDir;
                await handler(options);
            },
            _workingDirOption,
            new OptionsBinder(
                graphSampleCountOption: countOption,
                graphSampleModeOption: modeOption));

            return cmd;
        }

        private static Command GetTraverseCmd(Func<Options, Task> handler)
        {
            var cmd = new Command(
                name: "traverse",
                description: "TODO: add some description");
            cmd.AddCommand(GetBitcoinCmd(handler));

            return cmd;
        }

        private static Command GetBitcoinCmd(Func<Options, Task> handler)
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
                granularityOption: granularityOption));

            return cmd;
        }
    }
}
