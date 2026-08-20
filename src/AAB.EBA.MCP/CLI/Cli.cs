using AAB.EBA.CLI.Config;

namespace AAB.EBA.MCP.CLI;

internal class Cli
{
    private readonly RootCommand _rootCmd;
    private readonly Action<Exception, ParseResult> _exceptionHandler;

    public Cli(
        Func<Options, Task> runHandlerAsync,
        Action<Exception, ParseResult> exceptionHandler)
    {
        _exceptionHandler = exceptionHandler;

        var httpOption = new Option<bool>("--http")
        {
            Description =
                "Serve MCP over Streamable HTTP (http://localhost:5000) instead of the " +
                "default stdio transport. Used for standalone/manual testing, e.g. via " +
                "the MCP inspector."
        };

        _rootCmd = new RootCommand(description: "Runs the EBA Model Context Protocol server.")
        {
            httpOption
        };

        _rootCmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = new Options(Path.GetTempPath())
            {
                Mcp = new McpOptions { Http = parseResult.GetValue(httpOption) }
            };

            try
            {
                await runHandlerAsync(options);
            }
            catch (Exception e)
            {
                _exceptionHandler(e, parseResult);
            }
        });
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        var parseResult = _rootCmd.Parse(args);
        try
        {
            return await parseResult.InvokeAsync();
        }
        catch (Exception e)
        {
            _exceptionHandler(e, parseResult);
            return 1;
        }
    }
}
