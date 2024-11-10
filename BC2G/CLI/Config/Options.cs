namespace BC2G.CLI.Config;

public class Options
{
    public long Timestamp { init; get; } = _timestamp;
    public string WorkingDir { init; get; } = _wd;
    public string StatusFile { init; get; } = Path.Join(_wd, $"status_{_timestamp}.json");

    /// <summary>
    /// The value of this parameter should be set based on the performance of the 
    /// host machine, and the bitcoin agent throughput. 
    /// Setting it to a high number (e.g., 200), needs performant enough bitcoin agent,
    /// otherwise, you may experience an increased number of ServiceUnavailable errors. 
    /// Setting it to a low value (e.g., 2, .NET default) will considerable limit the 
    /// multi-threading since most threads operate on the API responses, and a low value
    /// will limit that.
    /// Also, setting it to a very high value may result in port exhaustion and may 
    /// interfere with other operation system network-related processes.
    /// </summary>
    public int DefaultConnectionLimit { init; get; } = 100;

    public LoggerOptions Logger { init; get; } =
        new()
        {
            // The `_` before `.log` is added to separate RepoName from a 
            // timestamp that serilog adds for each rolling file.
            LogFilename = Path.Join(_wd, $"{new LoggerOptions().RepoName}_.log")
        };

    public BitcoinOptions Bitcoin { init; get; } = new();
    public GraphSampleOptions GraphSample { init; get; } = new();
    public Neo4jOptions Neo4j { init; get; } = new();

    private static readonly long _timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
    private static readonly string _wd = Path.Join(Environment.CurrentDirectory, $"session_{_timestamp}");
}
