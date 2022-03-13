using BC2G.Serializers;
using Microsoft.Extensions.CommandLineUtils;
using System.Reflection;
using System.Text;

namespace BC2G.CLI
{
    public class CommandLineOptions
    {
        public string StatusFilename { get { return _statusFilename; } }
        private string _statusFilename = "status.json";

        private readonly CommandLineApplication _cla;

        private readonly CommandOption _fromOption = new(
            "-f | --from <value>",
            CommandOptionType.SingleValue)
        {
            Description = "Sets the height of the block where " +
            "the traverse should start (inclusive)."
        };

        private readonly CommandOption _toOption = new(
            "-t | --to <value>",
            CommandOptionType.SingleValue)
        {
            Description = "Sets the height of the block where " +
            "the traverse should end (exclusive)."
        };

        private readonly CommandOption _outputOption = new(
            "-o | --output <value>",
            CommandOptionType.SingleValue)
        {
            Description = "Sets a path where the result of " +
            "block traversal should be persisted."
        };

        private readonly CommandOption _statusFilenameOption = new(
            "-s | --status-filename <value>",
            CommandOptionType.SingleValue)
        {
            Description = "The file where the execution status " +
            "is presisted in JSON format."
        };

        private readonly CommandOption _addressIdMappingFilenameOption = new(
            "-m | --mapping-filename <value>",
            CommandOptionType.SingleValue)
        {
            Description = "The filename of transaction address " +
            "to its correspoinding ID mapping."
        };

        private readonly CommandOption _resumeFromOption = new(
            "-r | --resume-from <value>",
            CommandOptionType.SingleValue)
        {
            Description = "Resumes a canceled execution based " +
            "on the given status filename."
        };

        private readonly CommandOption _createPerBlockFilesOption = new(
            "-p | --create-per-block-files",
            CommandOptionType.NoValue)
        {
            Description = "If provided, for each block it traverses, " +
            "it creates `[block_height]_edges.csv` and `[block_height]_nodes.csv. "
        };

        private Options _parsedOptions = new();

        public static string HelpOption
        {
            get { return "-? | -h | --help"; }
        }

        public CommandLineOptions()
        {
            _cla = new CommandLineApplication
            {
                Name = "BC2G",
                FullName = "bitcoin Chain to Graph",
            };

            _cla.Options.Add(_fromOption);
            _cla.Options.Add(_toOption);
            _cla.Options.Add(_outputOption);
            _cla.Options.Add(_statusFilenameOption);
            _cla.Options.Add(_resumeFromOption);
            _cla.Options.Add(_createPerBlockFilesOption);

            var version = "Unknown (Called from unmanaged code)";
            var assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (attribute != null)
                    version = attribute.InformationalVersion;
            }

            _cla.HelpOption(HelpOption);
            _cla.VersionOption("-v | --version", () =>
            {
                return $"Version {version}";
            });

            Func<int> assertArguments = AssertArguments;
            _cla.OnExecute(assertArguments);
        }

        public Options Parse(string[] args, out bool helpOrVersionIsDisplayed)
        {
            helpOrVersionIsDisplayed = _cla.Execute(args) != 1;
            return _parsedOptions;
        }

        private int AssertArguments()
        {
            AssertRequiredArgsAreGiven();
            AssertGivenArgs();
            return 1;
        }

        private void AssertRequiredArgsAreGiven()
        {
            var missingArgs = new List<string>();
            // Any required argument?! 
            // For any required argument, check if a value is provided
            // and if not, add the argument to the missingArgs list.

            if (missingArgs.Count > 0)
            {
                var msgBuilder = new StringBuilder(
                    "The following required arguments are missing: ");
                for (int i = 0; i < missingArgs.Count - 1; i++)
                    msgBuilder.Append(missingArgs[i] + "; and ");
                msgBuilder.Append(missingArgs[^1] + ".");

                throw new ArgumentException(msgBuilder.ToString());
            }
        }

        private void AssertGivenArgs()
        {
            // TODO: if a value for this argument is given,
            // no value for other arguments should be
            // provided, throw a warning that other arguments
            // will be ignored. 
            if (_resumeFromOption.HasValue())
            {
                var resumeFromFilename = Path.GetFullPath(_resumeFromOption.Value());
                try
                {
                    _parsedOptions = JsonSerializer<Options>.DeserializeAsync(resumeFromFilename).Result;
                    return;
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Failed loading status from " +
                        $"`{resumeFromFilename}`: {e.Message}");
                }
            }

            _parsedOptions = new Options();

            int from = -1, to = -1;
            if (_fromOption.HasValue() &&
                !int.TryParse(_fromOption.Value(), out from))
                throw new ArgumentException(
                    $"Invalid value given for the " +
                    $"`{_fromOption.LongName}` argument.");

            if (_toOption.HasValue() &&
                !int.TryParse(_toOption.Value(), out to))
                throw new ArgumentException(
                    $"Invalid value given for the " +
                    $"`{_toOption.LongName}` argument.");

            if (to != -1 && from != -1 && to <= from)
                throw new ArgumentException(
                    $"Provided value for {_toOption.LongName} " +
                    $"({_toOption.Value}) should be greater " +
                    $"than the value provided for " +
                    $"{_fromOption.LongName} ({_fromOption.Value})");

            _parsedOptions.FromInclusive = from;
            _parsedOptions.ToExclusive = to;

            var output = string.Empty;
            if (_outputOption.HasValue())
            {
                try
                {
                    output = Path.GetFullPath(_outputOption.Value());
                    _parsedOptions.OutputDir = output;
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Invalid value given for the " +
                        $"`{_outputOption.LongName}` argument: {e.Message}");
                }
            }

            if (_statusFilenameOption.HasValue())
            {
                try
                {
                    _statusFilename = Path.GetFullPath(
                        _statusFilenameOption.Value());
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Invalid value given for the " +
                        $"`{_statusFilenameOption.LongName}` " +
                        $"argument: {e.Message}");
                }
            }
            else
            {
                _statusFilename = Path.Combine(output, _statusFilename);
            }

            if (_addressIdMappingFilenameOption.HasValue())
            {
                try
                {
                    _parsedOptions.AddressIdMappingFilename =
                        Path.GetFullPath(_addressIdMappingFilenameOption.Value());
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Invalid value given for the " +
                        $"`{_addressIdMappingFilenameOption.LongName}` " +
                        $"argument: {e.Message}");
                }
            }
            else
            {
                _parsedOptions.AddressIdMappingFilename =
                    Path.Combine(output, _parsedOptions.AddressIdMappingFilename);
            }


            if (_createPerBlockFilesOption.HasValue())
                _parsedOptions.CreatePerBlockFiles = true;
        }
    }
}
