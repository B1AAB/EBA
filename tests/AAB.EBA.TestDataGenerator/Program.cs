using AAB.EBA.TestDataGenerator;
using System.Diagnostics;

var config = CLI.ParseArgs(args);
if (config == null) return;

Directory.CreateDirectory(config.BitcoinCoreReponses);
Directory.CreateDirectory(config.ExpectedOutputDir);

using var httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8332/rest/") };
var apiMockGenerator = new ApiMockDataGenerator(httpClient);

if (config.Command == "expected_data" || config.Command == "all")
{
    foreach (var interval in config.Intervals)
    {
        Console.WriteLine($"Generating Expected Data for interval: {interval.From}-{interval.To}");

        var blockExpectedDir = Path.Combine(config.ExpectedOutputDir, Dirs.GetExpectedOutputDir(interval.From, interval.To));
        Directory.CreateDirectory(blockExpectedDir);

        await GenerateExpectedDataAsync(interval.From, interval.To, blockExpectedDir);
    }
}

if (config.Command == "api_mock" || config.Command == "all")
{
    await apiMockGenerator.GenerateChainInfoAsync(config.BitcoinCoreReponses);

    foreach (var interval in config.Intervals)
    {
        Console.WriteLine($"Generating API Mock Data for interval: {interval.From}-{interval.To}");        

        for (int i = interval.From; i <= interval.To; i++)
        {
            await apiMockGenerator.GenerateAsync(config.BitcoinCoreReponses, i);
        }
    }
}

Console.WriteLine("Successfully finished.");
Environment.Exit(0);

static async Task GenerateExpectedDataAsync(int from, int to, string outputDir)
{
    var processInfo = new ProcessStartInfo
    {
        FileName = "AAB.EBA.exe",
        Arguments = $"bitcoin traverse --from {from} --to {to} --working-dir \"{outputDir}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };

    using var process = Process.Start(processInfo);
    if (process != null)
    {
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"\nExited with errors (Code {process.ExitCode}):");
            Console.Error.WriteLine(error);
        }
        else
        {
            Console.WriteLine(output);
            Console.WriteLine($"Expected data successfully created. '{outputDir}'");
        }
    }
    else
    {
        Console.Error.WriteLine("Failed to launch AAB.EBA.exe process.");
    }
}
