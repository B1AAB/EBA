using System.Text.Json.Serialization;

namespace AAB.EBA.TestDataGenerator.Model;

public class TransactionMinimal
{
    [JsonPropertyName("txid")]
    public string Txid { set; get; } = string.Empty;

    [JsonPropertyName("hash")]
    public string Hash { set; get; } = string.Empty;

    [JsonPropertyName("vin")]
    public List<InputMinimal> Inputs { set; get; } = [];
}
