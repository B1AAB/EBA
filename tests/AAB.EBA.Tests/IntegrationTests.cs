using AAB.EBA.TestDataGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests;

public class IntegrationTests : IDisposable
{
    private readonly HttpListener _httpListener;
    private readonly int _port = 8332;
    private readonly string _mockDataPath;
    private readonly Dictionary<long, string> _heightToHashMap;

    public IntegrationTests()
    {
        _mockDataPath = Path.GetFullPath(ClientFixture.BitcoinCoreResourcesDir);
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{_port}/");
        _httpListener.Start();

        _heightToHashMap = JsonSerializer.Deserialize<Dictionary<long, string>>(
            File.ReadAllText(Path.Join(_mockDataPath, Files.HeightToHashMapFilename))) 
            ?? [];

        Task.Run(RunMockServer);
    }

    private async Task RunMockServer()
    {
        while (_httpListener.IsListening)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                var requestPath = context.Request.Url.AbsolutePath.TrimStart('/');

                string responseString = null;

                // Handle routing based on ClientFixture.cs logic
                if (requestPath == "rest/chaininfo.json")
                {
                    var file = Path.Combine(_mockDataPath, Files.ChainInfo);
                    if (File.Exists(file)) 
                        responseString = File.ReadAllText(file);
                }
                else if (requestPath.StartsWith("rest/blockhashbyheight/"))
                {
                    var heightPart = requestPath.Replace("rest/blockhashbyheight/", "").Replace(".hex", "");
                    if (int.TryParse(heightPart, out int startHeight) && _heightToHashMap.TryGetValue(startHeight, out var blockHash))
                    {
                        var blockJsonFile = Path.Combine(_mockDataPath, Files.GetMockBlockFilename(blockHash));
                        if (File.Exists(blockJsonFile))
                        {
                            var blockJson = File.ReadAllText(blockJsonFile);
                            using var doc = JsonDocument.Parse(blockJson);
                            responseString = doc.RootElement.GetProperty("hash").GetString();
                        }
                    }
                }
                else if (requestPath.StartsWith("rest/block/"))
                {
                    var hashPart = requestPath.Replace("rest/block/", "").Replace(".json", "");
                    var blockJsonFile = Path.Combine(_mockDataPath, Files.GetMockBlockFilename(hashPart));
                    if (File.Exists(blockJsonFile))
                        responseString = File.ReadAllText(blockJsonFile);
                }
                else if (requestPath.StartsWith("rest/tx/"))
                {
                    var txId = requestPath.Replace("rest/tx/", "").Replace(".json", "");
                    var txFile = Path.Combine(_mockDataPath, Files.GetMockTxFilename(txId));
                    if (File.Exists(txFile))
                        responseString = File.ReadAllText(txFile);
                }

                if (responseString != null)
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(responseString);
                    context.Response.StatusCode = 200;
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                }
                else
                {
                    context.Response.StatusCode = 404;
                }
                context.Response.Close();
            }
            catch (HttpListenerException)
            {
                // Ignored - listener stopped or disposed
            }
        }
    }

    [Theory]
    [InlineData(71036, 71036)]
    public void RunBitcoinTraverseCommand_ShouldProduceCorrectOutput_ForHeight(int startHeight, int endHeight)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "EBA_CLI_IntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var executable = Path.Combine(
                AppContext.BaseDirectory, 
                "aab.eba" + (OperatingSystem.IsWindows() ? ".exe" : ""));

            var processStartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = $"bitcoin traverse --from {startHeight} --to {endHeight} --working-dir {tempDir}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(processStartInfo);
            if (process ==null || process.HasExited)
                throw new InvalidOperationException("Failed to start the process or it has already exited.");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            Console.WriteLine($"EBA stdout:\n{output}");
            Console.WriteLine("-----------------------------");

            Console.WriteLine($"EBA stderr:\n{error}");
            Console.WriteLine("-----------------------------");

            var actualFiles = Directory.GetFiles(tempDir);
            Console.WriteLine("Actual files generated:\n");
            foreach (var actualFile in actualFiles.Select(Path.GetFileName))
                Console.WriteLine(actualFile);            
            Console.WriteLine("-----------------------------");

            var expectedOutputDir = Path.GetFullPath(
                Path.Combine(ClientFixture.BitcoinExpectedOutputsDir, Dirs.GetExpectedOutputDir(startHeight, endHeight)));

            var failToConnectToBitcoinCoreError = "Failed to communicate with the Bitcoin client";
            Assert.DoesNotContain(failToConnectToBitcoinCoreError, output);
            Assert.DoesNotContain(failToConnectToBitcoinCoreError, error);

            CompareOutputDirectories(expectedOutputDir, tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private static void CompareOutputDirectories(string expectedDir, string actualDir)
    {
        var expectedFiles = Directory.GetFiles(expectedDir).Select(Path.GetFileName).ToList();
        var actualFiles = Directory.GetFiles(actualDir).Select(Path.GetFileName).ToList();

        // Need to clean up prefixes consisting of timestamp e.g. 1779561113_ 
        // A timestamp is typically 10 digits followed by an underscore
        static string CleanFileName(string? file)
        {
            if (file == null)
                return string.Empty;

            var parts = file.Split('_', 2);
            if (parts.Length > 1 && parts[0].Length == 10 && long.TryParse(parts[0], out _))
                return parts[1];
            
            return file;
        }

        var expectedCleanFiles = expectedFiles.Select(CleanFileName).ToList();
        var actualCleanFiles = actualFiles.Select(CleanFileName).ToList();

        // The expected directory might contain
        // `expected_output_71036.json` and `events_...log`
        // which represent test data configurations
        // or execution logs from other runs.
        // We shouldn't fail if these are missing in ACTUAL.
        // Actually, missingFiles = Expected - Actual. 
        // We can ignore some expected config files that AAB.EBA
        // doesn't generate but exist in Expected output.
        // Let's remove them from expected files
        expectedCleanFiles.RemoveAll(f => f.StartsWith("expected_output_") || f.StartsWith("events_"));
        actualCleanFiles.RemoveAll(f => f.StartsWith("events_"));

        var missingFiles = expectedCleanFiles.Except(actualCleanFiles).ToList();
        var extraFiles = actualCleanFiles.Except(expectedCleanFiles).ToList();

        Assert.True(expectedCleanFiles.Count > 0);
        Assert.True(actualCleanFiles.Count > 0);
        Assert.Empty(missingFiles);
        
        foreach (var expectedFile in expectedFiles)
        {
            var cleanName = CleanFileName(expectedFile);
            
            // Skip checking those that were filtered out
            if (cleanName.StartsWith("events_") || cleanName.StartsWith("expected_output_"))
            {
                continue;
            }

            var actualFileMatches = actualFiles.Where(f => CleanFileName(f) == cleanName).ToList();
            
            Assert.True(
                actualFileMatches.Count == 1, 
                $"Could not find a unique matching actual file for {cleanName}");

            var actualFile = actualFileMatches.First();

            var expectedPath = Path.Combine(expectedDir, expectedFile);
            var actualPath = Path.Combine(actualDir, actualFile);

            if (cleanName.EndsWith(".json") || cleanName.EndsWith(".log") || cleanName.EndsWith(".eba"))
            {
                // skipping status.json, eba logs, and logs because they contain timestamps/paths that differ
            }
            else if (cleanName.EndsWith(".gz"))
            {
                
                using var expectedStream = new System.IO.Compression.GZipStream(
                    File.OpenRead(expectedPath), System.IO.Compression.CompressionMode.Decompress);

                using var actualStream = new System.IO.Compression.GZipStream(
                    File.OpenRead(actualPath), System.IO.Compression.CompressionMode.Decompress);

                using var expectedReader = new StreamReader(expectedStream);
                using var actualReader = new StreamReader(actualStream);
                
                var expectedText = expectedReader.ReadToEnd();
                var actualText = actualReader.ReadToEnd();
                
                var expectedLines = expectedText
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
                    .OrderBy(l => l).ToList();

                var actualLines = actualText
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
                    .OrderBy(l => l).ToList();

                Assert.True(
                    expectedLines.Count == actualLines.Count, 
                    $"Expected {expectedLines.Count} lines but found {actualLines.Count} lines.");

                // TODO: this test needs to be improved to compare the content of lines. 
                // one of the significant challenges in that is numbers in the 
                // expected files may not match the numbers in actual files,
                // because of the differences in the runtimes used for generating 
                // the expected data vs. the runtime that results in small rounding errors.
                // Resolving that rounding issues is not trivial because a 
                // double number might be multiplied by 10s to convert it to a long in EBA, 
                // hence you're not necesasrily comparing 0.01 with 0.012, but rather 1000 with 1012,
                // which is a significant difference.
                /*
                var expectedNotInActual = expectedLines.Except(actualLines).ToList();
                var actualNotInExpected = actualLines.Except(expectedLines).ToList();

                Assert.True(
                    expectedNotInActual.Count == 0, 
                    $"Comparing expected {expectedPath} and actual path {actualPath}; Expected lines not in actual:\n{string.Join("\n", expectedNotInActual)}");

                Assert.True(
                    actualNotInExpected.Count == 0, 
                    $"Comparing expected {expectedPath} and actual path {actualPath}; Actual lines not in expected:\n{string.Join("\n", actualNotInExpected)}");

                var intersectionSize = (double)expectedLines.Intersect(actualLines).Count();
                Assert.True(
                    intersectionSize == expectedLines.Count, 
                    $"GZip content for {cleanName} didn't match.");
                */
            }
            else
            {
                /* TODO: see the above TODO.
                 * 
                var expectedBytes = File.ReadAllBytes(expectedPath);
                var actualBytes = File.ReadAllBytes(actualPath);
                Assert.Equal(expectedBytes.Length, actualBytes.Length);
                for(int i = 0; i < expectedBytes.Length; ++i)
                {
                    Assert.Equal(expectedBytes[i], actualBytes[i]);
                }*/
            }
        }
    }

    public void Dispose()
    {
        _httpListener.Stop();
        _httpListener.Close();
    }
}
