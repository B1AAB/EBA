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

    private RecordedBlock(JsonElement block)
    {
        Height = block.GetProperty("height").GetInt64();
        Hash = block.GetProperty("hash").GetString()!;
        Transactions = [.. block.GetProperty("tx").EnumerateArray().Select(ReadTransaction)];
    }

    public static RecordedBlock FromMockData(long height)
    {
        using var document = JsonDocument.Parse(MockData.BlockJson(height));
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

    private static long Satoshi(JsonElement btc) => checked((long)(btc.GetDecimal() * 100_000_000m));
}
