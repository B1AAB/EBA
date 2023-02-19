namespace BC2G.CLI.Config;

public class Options
{
    public long Timestamp { init; get; } = _timestamp;
    public string WorkingDir { init; get; } = _wd;
    public string StatusFile { init; get; } = Path.Join(_wd, $"status_{_timestamp}.json");
    public int DefaultConnectionLimit { init; get; } = 50;
    public LoggerOptions Logger { init; get; } = new();
    public BitcoinOptions Bitcoin { init; get; } = new();
    public GraphSampleOptions GraphSample { init; get; } = new();
    public Neo4jOptions Neo4j { init; get; } = new();
    public PsqlOptions Psql { init; get; } = new();

    private static readonly long _timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
    private static readonly string _wd = Path.Join(Environment.CurrentDirectory, $"session_{_timestamp}");
}
