namespace AAB.EBA.CLI.Config;

public class McpOptions
{
    /// <summary>
    /// When true, serves MCP over Streamable HTTP instead of the default stdio transport. 
    /// </summary>
    public bool Http { init; get; } = false;
}
