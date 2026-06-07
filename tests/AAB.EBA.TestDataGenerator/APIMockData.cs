using System.Text.Json;

namespace AAB.EBA.TestDataGenerator;

public interface IApiMockDataGenerator
{
    Task GenerateChainInfoAsync(string outputDir);
    Task GenerateAsync(string outputDir, int blockHeight);
}

public class ApiMockDataGenerator(HttpClient client) : IApiMockDataGenerator
{
    private readonly HttpClient _client = client;
    private const int MaxConcurrency = 100;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = false
    };

    private static string GetMockDataDirectory(string dataCategory)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory != null && currentDirectory.GetFiles("EBA.sln").Length == 0)
            currentDirectory = currentDirectory.Parent;

        if (currentDirectory == null)
            throw new DirectoryNotFoundException(
                "Could not find the solution root directory containing EBA.sln.");        

        return Path.Combine(currentDirectory.FullName, Dirs.Tests, Dirs.MockData, Dirs.Bitcoin, dataCategory);
    }

    public async Task GenerateChainInfoAsync(string outputDir)
    {
        var targetDir = GetMockDataDirectory(Dirs.BitcoinCoreResponses);
        var chainInfo = (await _client.GetStringAsync("chaininfo.json")).Trim();

        Directory.CreateDirectory(targetDir);
        await File.WriteAllTextAsync(Path.Combine(targetDir, "chaininfo.json"), FormattedJson(chainInfo));
    }

    public async Task GenerateAsync(string outputDir, int blockHeight)
    {
        var responseDir = GetMockDataDirectory(Dirs.BitcoinCoreResponses);
        Directory.CreateDirectory(responseDir);

        var blockHash = (await _client.GetStringAsync($"blockhashbyheight/{blockHeight}.hex")).Trim();
        var blockEndpoint = $"block/{blockHash}.json";
        var blockJson = await _client.GetStringAsync(blockEndpoint);
        var block = JsonSerializer.Deserialize<Model.BlockMinimal>(blockJson);

        if (block == null)
            return;

        var heightToHashMap = new Dictionary<long, string>();
        var heightToHashMapFilename = Path.Combine(responseDir, Files.HeightToHashMapFilename);
        if (File.Exists(heightToHashMapFilename))
            heightToHashMap = JsonSerializer.Deserialize<Dictionary<long, string>>(
                await File.ReadAllTextAsync(heightToHashMapFilename))
                ?? [];

        heightToHashMap.TryAdd(blockHeight, blockHash);
        await File.WriteAllTextAsync(
            heightToHashMapFilename, 
            FormattedJson(JsonSerializer.Serialize(heightToHashMap, _jsonOptions)));

        await File.WriteAllTextAsync(
            Path.Combine(responseDir, Files.GetMockBlockFilename(blockHash)),
            FormattedJson(blockJson));

        var allTxIds = block.Transactions
            .Where(tx => !string.IsNullOrEmpty(tx.Txid))
            .Select(tx => tx.Txid!)
            .Distinct()
            .ToList();

        Console.WriteLine(
            $"Block {blockHeight}: {allTxIds.Count} native txs to download.");

        try
        {
            await Parallel.ForEachAsync(
                allTxIds,
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency },
                async (txId, ct) =>
                {
                    await ProcessTransactionAsync(responseDir, txId, ct);
                });
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: One or more transaction downloads failed for block {blockHeight}: {ex.Message}");
            Console.ResetColor();
        }
    }

    private async Task ProcessTransactionAsync(string targetDir, string txId, CancellationToken ct)
    {
        try
        {
            var exTx = await _client.GetStringAsync($"tx/{txId}.json", ct);
            var filename = Path.Combine(targetDir, Files.GetMockTxFilename(txId));
            await File.WriteAllTextAsync(filename, FormattedJson(exTx), ct);
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Failed to fetch transaction {txId}: {ex.Message}");
            Console.ResetColor();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Timeout: Transaction {txId}: {ex.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unexpected error: Transaction {txId}: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static string FormattedJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, _jsonOptions);
    }
}