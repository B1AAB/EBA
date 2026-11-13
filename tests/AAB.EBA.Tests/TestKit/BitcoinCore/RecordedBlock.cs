using AAB.EBA.TestDataGenerator;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

public sealed record RecordedTransaction(
    string Id, bool IsCoinbase, long InputValue, long OutputValue, long Fee,
    int InputCount, int OutputCount, IReadOnlyList<string> SourceTransactionIds);

public sealed class RecordedBlock
{
    public long Height { get; }
    public string Hash { get; }
    public IReadOnlyList<RecordedTransaction> Transactions { get; }
    public RecordedTransaction Coinbase => Transactions.Single(x => x.IsCoinbase);
    public IReadOnlyDictionary<string, long> InputScriptTypeCounts { get; }
    public IReadOnlyDictionary<string, long> OutputScriptTypeCounts { get; }
    public IReadOnlyDictionary<string, long> InputScriptTypeValues { get; }
    public IReadOnlyDictionary<string, long> OutputScriptTypeValues { get; }

    private RecordedBlock(JsonElement block)
    {
        Height = block.GetProperty("height").GetInt64();
        Hash = block.GetProperty("hash").GetString()!;
        Transactions = [.. block.GetProperty("tx").EnumerateArray().Select(ReadTransaction)];
        var outputs = block.GetProperty("tx").EnumerateArray()
            .SelectMany(x => x.GetProperty("vout").EnumerateArray()).ToArray();
        var inputs = block.GetProperty("tx").EnumerateArray()
            .SelectMany(x => x.GetProperty("vin").EnumerateArray())
            .Where(x => !x.TryGetProperty("coinbase", out _))
            .Select(x => x.GetProperty("prevout")).ToArray();
        InputScriptTypeCounts = Profile(inputs, values: false);
        OutputScriptTypeCounts = Profile(outputs, values: false);
        InputScriptTypeValues = Profile(inputs, values: true);
        OutputScriptTypeValues = Profile(outputs, values: true);
    }

    public static RecordedBlock FromMockData(long height)
    {
        var directory = ClientFixture.BitcoinCoreResourcesDir;
        var hashes = JsonSerializer.Deserialize<Dictionary<long, string>>(
            File.ReadAllText(Path.Combine(directory, Files.HeightToHashMapFilename)))!;
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(directory, Files.GetMockBlockFilename(hashes[height]))));
        return new RecordedBlock(document.RootElement);
    }

    private static RecordedTransaction ReadTransaction(JsonElement tx)
    {
        var inputs = tx.GetProperty("vin").EnumerateArray().ToArray();
        var outputs = tx.GetProperty("vout").EnumerateArray().ToArray();
        var coinbase = inputs.Any(x => x.TryGetProperty("coinbase", out _));
        return new RecordedTransaction(
            tx.GetProperty("txid").GetString()!, coinbase,
            coinbase ? 0 : inputs.Sum(x => Satoshi(x.GetProperty("prevout").GetProperty("value"))),
            outputs.Sum(x => Satoshi(x.GetProperty("value"))),
            coinbase ? 0 : Satoshi(tx.GetProperty("fee")),
            coinbase ? 0 : inputs.Length, outputs.Length,
            coinbase ? [] : inputs.Select(x => x.GetProperty("txid").GetString()!).ToArray());
    }

    private static Dictionary<string, long> Profile(IEnumerable<JsonElement> outputs, bool values)
    {
        return outputs.GroupBy(x => x.GetProperty("scriptPubKey").GetProperty("type").GetString()!)
            .ToDictionary(x => x.Key, x => values ? x.Sum(y => Satoshi(y.GetProperty("value"))) : x.LongCount());
    }

    private static long Satoshi(JsonElement btc) => checked((long)(btc.GetDecimal() * 100_000_000m));
}
