namespace AAB.EBA.TestDataGenerator;

public class CLI
{
    public static Config? ParseArgs(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Missing args. Usage:");
            Console.Error.WriteLine("  AAB.EBA.TestDataGenerator <command> <intervals>");
            Console.Error.WriteLine("Commands: all, expected_data, api_mock");
            Console.Error.WriteLine("Example : all 0-10,10-20");
            Environment.Exit(1);
            return null;
        }

        var cmd = args[0].ToLower();
        if (cmd != "all" && cmd != "expected_data" && cmd != "api_mock")
        {
            Console.Error.WriteLine($"Invalid command '{cmd}'; expected: all, expected_data, api_mock");
            Environment.Exit(1);
            return null;
        }

        var ranges = args[1].Split(",");
        var intervals = new List<Interval>();
        foreach (var range in ranges)
        {
            var s = range.Split("-");
            intervals.Add(new Interval { From = int.Parse(s[0]), To = int.Parse(s[1]) });
        }

        var slnRoot = GetSolutionRootDirectory();
        var baseMockDataDir = Path.Combine(slnRoot, "tests", "MockData", "Bitcoin");

        return new Config
        {
            Command = cmd,
            Intervals = intervals,
            BitcoinCoreReponses = Path.Combine(baseMockDataDir, "BitcoinCoreResponses"),
            ExpectedOutputDir = Path.Combine(baseMockDataDir, "ExpectedOutputs")
        };
    }

    public static string GetSolutionRootDirectory()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory != null && currentDirectory.GetFiles("EBA.sln").Length == 0)
        {
            currentDirectory = currentDirectory.Parent;
        }

        if (currentDirectory == null)
        {
            throw new DirectoryNotFoundException("Could not find the solution root directory containing EBA.sln.");
        }

        return currentDirectory.FullName;
    }
}
