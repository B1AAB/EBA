using AAB.EBA.CLI.Config;
using AAB.EBA.TestDataGenerator;
using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Traverse;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests;

[Trait("Category", "Integration")]
public class IntegrationTests : IDisposable
{
    private readonly HttpListener _httpListener;
    private readonly int _port = GetFreePort();
    private readonly HttpClient _nodeClient = new(RecordedBitcoinNode.FromMockData());

    public IntegrationTests()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{_port}/");
        _httpListener.Start();

        Task.Run(RunMockServer);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task RunMockServer()
    {
        while (_httpListener.IsListening)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                using var response = await _nodeClient.GetAsync(context.Request.Url ?? throw new InvalidOperationException("Missing request URL."));
                var bytes = await response.Content.ReadAsByteArrayAsync();
                context.Response.StatusCode = (int)response.StatusCode;
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
            catch (HttpListenerException)
            {
                // Ignored - listener stopped or disposed
            }
            catch (ObjectDisposedException)
            {
                // Ignored - listener stopped or disposed
            }
        }
    }

    [Theory]
    [InlineData(71036, 71036)]
    [InlineData(132317, 132317)]
    [InlineData(717531, 717531)]
    public async Task RunBitcoinTraverseCommand_ShouldProduceCorrectOutput_ForHeight(int startHeight, int endHeight)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "EBA CLI IntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var options = TraverseOptionsBuilder.ForWorkingDir(tempDir)
                .WithRange(startHeight, endHeight)
                .WithClientUri(new Uri($"http://localhost:{_port}/rest/"))
                .Build();
            File.WriteAllText(options.StatusFile, JsonSerializer.Serialize(options, Options.JsonSerializationOptions));

            var executable = Path.Combine(
                AppContext.BaseDirectory, 
                "aab.eba" + (OperatingSystem.IsWindows() ? ".exe" : ""));

            var processStartInfo = new ProcessStartInfo
            {
                FileName = executable,
                ArgumentList = { "bitcoin", "traverse", "--status-filename", options.StatusFile },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start the process.");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            Console.WriteLine($"EBA stdout:\n{output}");
            Console.WriteLine("-----------------------------");

            Console.WriteLine($"EBA stderr:\n{error}");
            Console.WriteLine("-----------------------------");

            Assert.True(process.ExitCode == 0,
                $"EBA exited with code {process.ExitCode}.\nstdout:\n{output}\nstderr:\n{error}");

            var actualFiles = Directory.GetFiles(tempDir);
            Console.WriteLine("Actual files generated:\n");
            foreach (var actualFile in actualFiles.Select(Path.GetFileName))
                Console.WriteLine(actualFile);            
            Console.WriteLine("-----------------------------");

            ExpectedGraphStore.Compare(startHeight, await TraverseGraphOutput.ReadAsync(options));

            var failToConnectToBitcoinCoreError = "Failed to communicate with the Bitcoin client";
            Assert.DoesNotContain(failToConnectToBitcoinCoreError, output);
            Assert.DoesNotContain(failToConnectToBitcoinCoreError, error);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    public void Dispose()
    {
        _httpListener.Stop();
        _httpListener.Close();
        _nodeClient.Dispose();
    }
}
