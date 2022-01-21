using BC2G.Serializers;
using Microsoft.Extensions.CommandLineUtils;
using System.Reflection;
using System.Text;

namespace BC2G.CLI
{
    public class CommandLineOptions
    {
        public string StatusFilename { get { return _statusFilename; } }
        public string? ResumeFrom { get { return _resumeFrom; } }

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

        //from = 719000;
        //to = 719010;
        private int _from = -1;
        private int _to = -1;
        private string _output = Environment.CurrentDirectory;
        private string _statusFilename = "status.json";
        private string _addressIdMappingFilename = "address_id_mapping.csv";
        private string? _resumeFrom = null;

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

            var version = "Unknown (Called from unmanaged code)";
            if (Assembly.GetEntryAssembly() != null)
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                version = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.

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
            if (helpOrVersionIsDisplayed)
                return new Options();

            if (ResumeFrom == null)
            {
                return new Options()
                {
                    FromInclusive = _from,
                    ToExclusive = _to,
                    OutputDir = _output,
                    AddressIdMappingFilename = _addressIdMappingFilename,
                };
            }
            else
            {
                try
                {
                    return JsonSerializer<Options>
                        .DeserializeAsync(ResumeFrom).Result;
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Failed loading status from " +
                        $"`{ResumeFrom}`: {e.Message}");
                }
            }
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
                    "the following required arguments are missing: ");
                for (int i = 0; i < missingArgs.Count - 1; i++)
                    msgBuilder.Append(missingArgs[i] + "; and ");
                msgBuilder.Append(missingArgs[^1] + ".");

                throw new ArgumentException(msgBuilder.ToString());
            }
        }

        private void AssertGivenArgs()
        {
            if (_fromOption.HasValue() && 
                !int.TryParse(_fromOption.Value(), out _from))
                throw new ArgumentException(
                    $"Invalid value given for the " +
                    $"`{_fromOption.LongName}` argument.");

            if (_toOption.HasValue() &&
                !int.TryParse(_toOption.Value(), out _to))
                throw new ArgumentException(
                    $"Invalid value given for the " +
                    $"`{_toOption.LongName}` argument.");

            if (_to != -1 && _from != -1 && _to <= _from)
                throw new ArgumentException(
                    $"Provided value for {_toOption.LongName} " +
                    $"({_toOption.Value}) should be greater " +
                    $"than the value provided for " +
                    $"{_fromOption.LongName} ({_fromOption.Value})");

            if (_outputOption.HasValue())
            {
                try
                {
                    _output = Path.GetFullPath(_outputOption.Value());
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        $"Invalid value given for the " +
                        $"`{_outputOption.LongName}` argument: {ex.Message}");
                }
            }

            if (_statusFilenameOption.HasValue())
            {
                try
                {
                    _statusFilename = Path.GetFullPath(
                        _statusFilenameOption.Value());
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        $"Invalid value given for the " +
                        $"`{_statusFilenameOption.LongName}` " +
                        $"argument: {ex.Message}");
                }
            }
            else
            {
                _statusFilename = Path.Combine(_output, _statusFilename);
            }

            if (_addressIdMappingFilenameOption.HasValue())
            {
                try
                {
                    _addressIdMappingFilename = Path.GetFullPath(
                        _addressIdMappingFilenameOption.Value());
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        $"Invalid value given for the " +
                        $"`{_addressIdMappingFilenameOption.LongName}` " +
                        $"argument: {ex.Message}");
                }
            }
            else
            {
                _addressIdMappingFilename = Path.Combine(
                    _output, _addressIdMappingFilename);
            }

            // TODO: if a value for this argument is given,
            // no value for other arguments should also be
            // provided, throw a warning that other arguments
            // will be ignored. Also, implement the code to
            // ignore those arguments.
            if(_resumeFromOption.HasValue())
            {
                try
                {
                    _resumeFrom = Path.GetFullPath(_resumeFromOption.Value());
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        $"Invalid value given for the " +
                        $"`{_resumeFromOption.LongName}` " +
                        $"argument: {ex.Message}");
                }
            }
        }
    }
}
