using System.Text.Json;

namespace AAB.EBA.MCP.Infrastructure;

public static class McpJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly JsonSerializerOptions Compact = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
