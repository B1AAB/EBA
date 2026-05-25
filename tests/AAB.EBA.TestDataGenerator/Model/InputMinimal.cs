using System.Text.Json.Serialization;

namespace AAB.EBA.TestDataGenerator.Model;

public class InputMinimal
{
    [JsonPropertyName("txid")]
    public string? Txid { get; set; }
}
