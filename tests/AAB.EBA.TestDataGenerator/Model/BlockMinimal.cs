using System.Text.Json.Serialization;

namespace AAB.EBA.TestDataGenerator.Model;

public class BlockMinimal
{
    [JsonPropertyName("hash")]
    public string Hash { set; get; } = string.Empty;

    [JsonPropertyName("tx")]
    public List<TransactionMinimal> Transactions { set; get; } = [];
}
