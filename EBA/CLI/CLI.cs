using Spectre.Console;
using System.CommandLine.Help;
using SecurityException = System.Security.SecurityException;

namespace EBA.CLI;

internal class Cli
{
    private readonly RootCommand _rootCmd;
    private readonly Option<string> _workingDirOption;
    private readonly Option<string> _statusFilenameOption;
    private readonly Action<Exception, ParseResult> _exceptionHandler;

    private readonly Options _defOps;

    public Cli(
        Func<Options, Task> bitcoinTraverseCmdHandlerAsync,
        Func<Options, Task> bitcoinDeDupCmdHandlerAsync,
        Func<Options, Task> bitcoinSampleCmdHandlerAsync,
        Func<Options, Task> bitcoinImportCmdHandlerAsync,
        Func<Options, Task> bitcoinMapMarketHandlerAsync,
        Func<Options, Task> bitcoinAddressStatsHandlerAsync,
        Func<Options, Task> bitcoinImportCypherQueriesAsync,
        Action<Exception, ParseResult> exceptionHandler)
    {
        _exceptionHandler = exceptionHandler;

        var defOps = new Options();
        _defOps = defOps;

        _workingDirOption = new Option<string>("--working-dir")
        {
            Description =
                "The directory where all the data related " +
                "to this execution will be stored.",
            DefaultValueFactory = _ => defOps.WorkingDir,
            Recursive = true,
            Validators =
            {
                result =>
                {
                    var value = result.GetValueOrDefault<string>();
                    if (value is null)
                    {
                        result.AddError("Working directory cannot be null");
                    }
                    else
                    {
                        try
                        {
                            _ = Path.GetFullPath(value);
                        }
                        catch (Exception e) when (
                            e is ArgumentException ||
                            e is SecurityException ||
                            e is NotSupportedException ||
                            e is PathTooLongException)
                        {
                            result.AddError($"Invalid path '{value}'");
                            return;
                        }
                    }
                }
            }
        };

        _statusFilenameOption = new Option<string>("--status-filename")
        {
            Description =
                "A JSON file to store the options used to run EBA. " +
                "If the file exists, all the options are read from the JSON file " +
                "and the default values used for any missing options, override all " +
                "the options set in the command line.",
            Recursive = true,
            DefaultValueFactory = _ => defOps.StatusFile
        };

        _rootCmd = new RootCommand(description: "Runs the graph ETL pipeline on cryptocurrencies.")
        {
            _workingDirOption,
            _statusFilenameOption,
            GetBitcoinCmd(
                defaultOptions: defOps,
                traverseHandlerAsync: bitcoinTraverseCmdHandlerAsync,
                dedupHandlerAsync: bitcoinDeDupCmdHandlerAsync,
                importHandlerAsync: bitcoinImportCmdHandlerAsync,
                sampleHandlerAsync: bitcoinSampleCmdHandlerAsync,
                mapMarketHandlerAsync: bitcoinMapMarketHandlerAsync,
                addressStatsHandlerAsync: bitcoinAddressStatsHandlerAsync,
                importCypherQueriesAsync: bitcoinImportCypherQueriesAsync)
        };

        for (int i = 0; i < _rootCmd.Options.Count; i++)
        {
            if (_rootCmd.Options[i] is HelpOption defaultHelpOption)
            {
                defaultHelpOption.Action = new CustomHelpAction((HelpAction)defaultHelpOption.Action!);
                break;
            }
        }
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        var parseResult = _rootCmd.Parse(args);
        try
        {
            return await parseResult.InvokeAsync();
        }
        catch (Exception e)
        {
            _exceptionHandler(e, parseResult);
            return 1;
        }
    }

    private Command GetBitcoinCmd(
        Options defaultOptions,
        Func<Options, Task> traverseHandlerAsync,
        Func<Options, Task> dedupHandlerAsync,
        Func<Options, Task> importHandlerAsync,
        Func<Options, Task> sampleHandlerAsync,
        Func<Options, Task> mapMarketHandlerAsync,
        Func<Options, Task> addressStatsHandlerAsync,
        Func<Options, Task> importCypherQueriesAsync)
    {
        var cmd = new Command(
            name: "bitcoin",
            description: "Implements methods for working with the Bitcoin blockchain.")
        {
            GetBitcoinTraverseCmd(defaultOptions, traverseHandlerAsync),
            GetBitcoinDedupCmd(defaultOptions, dedupHandlerAsync),
            GetBitcoinImportCmd(defaultOptions, importHandlerAsync),
            GetBitcoinImportCypherQueriesCmd(defaultOptions, importCypherQueriesAsync),
            GetBitcoinMapMarketCmd(defaultOptions, mapMarketHandlerAsync),
            GetBitcoinSampleCmd(defaultOptions, sampleHandlerAsync),
            GetBitcoinAddressStatsCmd(defaultOptions, addressStatsHandlerAsync)
        };
        return cmd;
    }

    private Command GetBitcoinTraverseCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var fromOption = new Option<int>("--from")
        {
            DefaultValueFactory = _ => 0,
            Description =
                "The height of the block where the " +
                "traverse should start. If not provided, starts from the " +
                "first block on the blockchain."
        };

        var toOption = new Option<int?>("--to")
        {
            Description =
                "The height of the block where the " +
                "traverse should end. If not provided, proceeds " +
                "until the last of block on the chain when the process starts."
        };

        var blocksListFile = new Option<string?>("--blocks-list-file")
        {
            Description =
                "A text file containing the list of block heights to traverse, " +
                "with one block per line. " +
                $"If provided, it will override the {fromOption.Name} and {toOption.Name} options."
        };

        var granularityOption = new Option<int>("--granularity")
        {
            DefaultValueFactory = _ => defaultOptions.Bitcoin.Traverse.Granularity,
            Description =
                "Set the blockchain traversal granularity." +
                "For instance, if set to 10, it implies processing every 10 blocks in the blockchain."
        };

        var clientUriOption = new Option<Uri>("--client-uri")
        {
            DefaultValueFactory = _ => defaultOptions.Bitcoin.Traverse.ClientUri,
            Description =
                "The URI where the Bitcoin client can be reached. The client should " +
                "be started with REST endpoint enabled."
        };

        var trackTxoOption = new Option<bool>("--track-txo")
        {
            Description =
                "If set, writes the list of txo it sees to a text file, this file will need to further processed" +
                "and it will also add to storage requirements. " +
                "Enabling this will slow down the traverse (e.g., from 7h to 11h for the first 500k blocks), and additional storage " +
                "requirements (e.g., ~140GB for the first 500k blocks) that needs post-traverse processing. Aggregated stats about Txo " +
                "are recoded in block stats, so set this flag only if you need the complete list of spent and unspent Tx outputs."
        };

        var txoFilenameOption = new Option<string>("--txo-filename")
        {
            Description =
                "Sets the filename used when the txo-persistence-policy is set to PersistToFileOnly."
        };

        var skipGraphSerialization = new Option<bool>("--skip-graph-serialization")
        {
            Description =
                "If provided, it skips writting per-block graphs to files. " +
                "This option is used when other stats are intended to be collected from traverse (e.g., per-block summary stats)"
        };

        var maxBlocksInBufferOption = new Option<int>("--max-blocks-in-buffer")
        {
            Description =
                "[Advanced] max number of blocks in the serialization buffer. " +
                "Lower values means buffer will be flushed more frequently, higher values means it will wait less frequently for the buffer to empty." +
                "Buffer flushing speed depends on how the persistance media's performance on serliazing objects, the faster it is, " +
                "the buffer will fill less frequently. " +
                "Memory footprint of buffer is a function of the size of each data for eacch block to be seriliazed in the buffer," +
                "such that earlier blocks with fewer Tx will have smaller footprint, and recent blocks with more tx will " +
                "have more per-block memory requirement."
        };

        var maxEntriesPerBatch = new Option<int>("--max-entries-per-batch")
        {
            Description =
                "[Advanced] Mainly related to importing data into a Neo4j database. " +
                "Sets the total number of nodes and edges serialized to one batch of CSV files. " +
                "The smaller this number is, the nodes and edges are serialized to more batches, hence more files; " +
                "the larger the number is, fewer larger files will be created. If you intent to use neo4j admin to " +
                "bulk import data into an empty database, then you may use a larger value for this parameter and " +
                "have fewer larger files. Otherwise, if you intent to use incremental import, it is better to use " +
                "smaller values. Neoj4 recomments 10k-100k if you intent to use incremental import. " +
                "(reference: https://neo4j.com/blog/bulk-data-import-neo4j-3-0/)"
        };

        var cmd = new Command(
            name: "traverse",
            description: "Traverses the blockchain in the defined range collecting the set metrics.")
        {
            fromOption,
            toOption,
            blocksListFile,
            granularityOption,
            clientUriOption,
            maxBlocksInBufferOption,
            txoFilenameOption,
            skipGraphSerialization,
            trackTxoOption,
            maxEntriesPerBatch
        };


        cmd.Validators.Add(commandResult =>
        {
            var errors = new List<string>();
            var fromValue = commandResult.GetValue(fromOption);
            var toValue = commandResult.GetValue(toOption);
            var blocksListFileValue = commandResult.GetValue(blocksListFile);

            var fromResult = commandResult.GetResult(fromOption);
            var toResult = commandResult.GetResult(toOption);
            var blocksListFileResult = commandResult.GetResult(blocksListFile);

            bool fromProvidedByUser = fromResult is not null && !fromResult.Implicit;
            bool toProvidedByUser = toResult is not null && !toResult.Implicit;
            bool blocksListDirProvidedByUser = blocksListFileResult is not null && !blocksListFileResult.Implicit;

            if (blocksListDirProvidedByUser && (fromProvidedByUser || toProvidedByUser))
                commandResult.AddError($"Options --{fromOption.Name} and --{toOption.Name} cannot be used together with --{blocksListFile.Name}.");

            if (fromValue < 0)
                commandResult.AddError($"Option --{fromOption.Name} must be a non-negative integer.");

            if (toValue is not null && toValue < 0)
                commandResult.AddError($"Option --{toOption.Name} must be a non-negative integer.");

            if (toValue is not null && fromValue > toValue)
                commandResult.AddError($"Option --{fromOption.Name} cannot be greater than option --{toOption.Name}.");

            if (blocksListDirProvidedByUser && !File.Exists(blocksListFileValue))
                commandResult.AddError($"The file specified in --{blocksListFile.Name} does not exist: {blocksListFileValue}.");
        });

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = OptionsBinder.Build(
                parseResult,
                fromOption: fromOption,
                toOption: toOption,
                blocksListFileOption: blocksListFile,
                granularityOption: granularityOption,
                bitcoinClientUri: clientUriOption,
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption,
                maxBlocksInBufferOption: maxBlocksInBufferOption,
                trackTxoOption: trackTxoOption,
                txoFilenameOption: txoFilenameOption,
                skipGraphSerializationOption: skipGraphSerialization,
                maxEntriesPerBatch: maxEntriesPerBatch);

            try
            {
                await handlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });

        return cmd;
    }

    private Command GetBitcoinDedupCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var scriptNodesFileOption = new Option<string>("--sorted-script-nodes-file")
        {
            Description = "The file containing sorted Script nodes."
        };

        var txNodesFileOption = new Option<string>("--sorted-tx-nodes-file")
        {
            Description = "The file containing sorted Tx nodes files."
        };

        var cmd = new Command(
            name: "dedup",
            description:
                "Deduplicates sorted Script and Tx node files. " +
                "See documentation at the following link on the rational " +
                "and sorting the files. https://eba.b1aab.ai/docs/bitcoin/etl/traverse")
        {
            scriptNodesFileOption,
            txNodesFileOption
        };

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = OptionsBinder.Build(
                parseResult,
                sortedScriptNodeFilenameOption: scriptNodesFileOption,
                sortedTxNodeFilenameOption: txNodesFileOption,
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption);

            try
            {
                await handlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });

        return cmd;
    }

    private Command GetBitcoinImportCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var batchFilenameOption = new Option<string>("--batch-filename")
        {
            Required = true,
            Description = "Sets the batch filename path."
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

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = OptionsBinder.Build(
                parseResult,
                batchFilenameOption: batchFilenameOption,
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption);

            try
            {
                await handlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });

        return cmd;
    }

    private Command GetBitcoinMapMarketCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var marketDataFilenameOption = new Option<string>("--market-data-filename")
        {
            Description = "OHLC and should be sorted",
            Required = true
        };

        var outputFilenameOption = new Option<string>("--output-filename")
        {
            Description = "The output file containing the mapping between block timestamps and market data.",
            Required = true
        };

        var cmd = new Command(
            name: "map-market",
            description: "...")
        {
            marketDataFilenameOption,
            outputFilenameOption
        };

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = OptionsBinder.Build(
                parseResult,
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption,
                marketDataFilenameOption: marketDataFilenameOption,
                outputFilenameOption: outputFilenameOption);
            try
            {
                await handlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });

        return cmd;
    }

    private Command GetBitcoinImportCypherQueriesCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var cmd = new Command(
            name: "cypher-queries",
            description: "Writes Neo4j Cypher queries used to import data from batches into a neo4j graph database.");

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = OptionsBinder.Build(
                parseResult,
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption);

            try
            {
                await handlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });

        return cmd;
    }

    private Command GetBitcoinSampleCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var countOption = new Option<int>("--count")
        {
            Description = "Sets the number of communities to sample."
        };

        var methodsAliases = new Dictionary<GraphTraversal, string[]>
        {
            {
                GraphTraversal.FFS,
                [
                    "Forest-Fire"
                ]
            }
        };

        var methodOption = new Option<GraphTraversal>("--method")
        {
            Description =
                "Sets the sampling method; currently supported methods are: " +
                "{" +
                    string.Join(", ", methodsAliases.Select(kvp => $"[{kvp.Value[0]} ({kvp.Key})]")) +
                "}",
            CustomParser = x =>
            {
                var def = new Options().Bitcoin.GraphSample.TraversalAlgorithm;
                if (x.Tokens.Count == 0)
                    return def;

                var providedValue = x.Tokens.Single().Value;
                if (Enum.TryParse<GraphTraversal>(providedValue, ignoreCase: true, out var parsedValue))
                {
                    return parsedValue;
                }
                else
                {
                    x.AddError($"Invalid --method provided: '{providedValue}'");
                    return def;
                }
            }
        };

        var methodOptionsOption = new Option<string>("--method-options")
        {
            Description = "A JSON string containing method-specific options."
        };

        var minNodeCountOption = new Option<int>("--min-node-count")
        {
            DefaultValueFactory = _ => defaultOptions.Bitcoin.GraphSample.MinNodeCount,
            Description = "Sets the minimum number of nodes in each sampled subgraph."
        };

        var maxNodeCountOption = new Option<int>("--max-node-count")
        {
            DefaultValueFactory = _ => defaultOptions.Bitcoin.GraphSample.MaxNodeCount,
            Description = "Sets the maximum number of nodes in each sampled subgraph."
        };

        var minEdgeCountOption = new Option<int>("--min-edge-count")
        {
            DefaultValueFactory = _ => defaultOptions.Bitcoin.GraphSample.MinEdgeCount,
            Description = "Sets the minimum number of edges in each sampled subgraph."
        };

        var maxEdgeCountOption = new Option<int>("--max-edge-count")
        {
            DefaultValueFactory = _ => defaultOptions.Bitcoin.GraphSample.MaxEdgeCount,
            Description = "Sets the maximum number of edges in each sampled subgraph."
        };

        var rootNodeSelectProbOption = new Option<double>("--root-node-select-prob")
        {
            DefaultValueFactory = _ => defaultOptions.Bitcoin.GraphSample.RootNodeSelectProb,
            Description =
                "Sets the sampling rate for root nodes. Accepts values from 0.0 to 1.0; " +
                "invalid inputs are replaced by the default configuration."
        };

        var cmd = new Command(
            name: "sample",
            description:
                "Methods for sampling from the graph. " +
                "Please refer to the following documentation for detailed description of the arguments: " +
                "https://eba.b1aab.ai/docs/bitcoin/sampling/overview")
        {
            countOption,
            minNodeCountOption,
            maxNodeCountOption,
            minEdgeCountOption,
            maxEdgeCountOption,
            rootNodeSelectProbOption,
            methodOption,
            methodOptionsOption,
        };

        cmd.Validators.Add(commandResult =>
        {
            if (commandResult.GetValue(_statusFilenameOption) == _defOps.StatusFile)
            {
                if (commandResult.GetResult(countOption) == null)
                    commandResult.AddError("Option '--count' is required when --status-filename is not used.");

                if (commandResult.GetResult(methodOption) == null)
                    commandResult.AddError($"Option '--{methodOption.Name}' is required when --status-filename is not used.");
            }

            var methodOptionsJson = commandResult.GetValue(methodOptionsOption);
            if (commandResult.GetValue(methodOption) == GraphTraversal.FFS
                && !string.IsNullOrWhiteSpace(methodOptionsJson))
            {
                try
                {
                    var serializationOptions = new JsonSerializerOptions
                    {
                        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                        PropertyNameCaseInsensitive = true
                    };
                    JsonSerializer.Deserialize<BitcoinForestFireOptions>(methodOptionsJson, serializationOptions);
                }
                catch (JsonException e)
                {
                    commandResult.AddError($"Invalid JSON for the Forest Fire sampling method options: {e.Message}");
                }
            }
        });

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = OptionsBinder.Build(
                parseResult,
                graphSampleCountOption: countOption,
                graphSampleMinNodeCount: minNodeCountOption,
                graphSampleMaxNodeCount: maxNodeCountOption,
                graphSampleMinEdgeCount: minEdgeCountOption,
                graphSampleMaxEdgeCount: maxEdgeCountOption,
                graphSampleMethodOption: methodOption,
                graphSampleMethodOptionsOption: methodOptionsOption,
                graphSampleRootNodeSelectProb: rootNodeSelectProbOption,
                workingDirOption: _workingDirOption,
                statusFilenameOption: _statusFilenameOption);

            try
            {
                await handlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });

        return cmd;
    }

    // TODO: 
    // since stats is not recorded separately during traverse (recorded as block node properties),
    // hence this command and all its related methods need to be re-thought.
    private Command GetBitcoinAddressStatsCmd(Options defaultOptions, Func<Options, Task> handlerAsync)
    {
        var addressesFilenameOption = new Option<string>("--addresses-filename")
        {
            Description = "File containing addresses in each block."
        };

        var cmd = new Command(
            name: "addresses-to-stats",
            description: "Extends the per-block stats with statistics about the " +
            "addresses computed from the file containing addresses in each block.")
        {
            addressesFilenameOption,
        };


        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = OptionsBinder.Build(
                parseResult,
                txoFilenameOption: addressesFilenameOption);

            try
            {
                await handlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });

        return cmd;
    }
}
