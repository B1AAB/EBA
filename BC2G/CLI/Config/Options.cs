namespace BC2G.Model.Config;

public class Options
{
    public long TimeStamp { get; }
    public string WorkingDir { set; get; } =
        Environment.CurrentDirectory;

    public string StatusFile { set; get; } =
        Path.Combine(Environment.CurrentDirectory, "status.json");

    public LoggerOptions Logger { set; get; } = new();
    public BitcoinOptions Bitcoin { set; get; } = new();
    public GraphSampleOptions GraphSample { set; get; } = new();
    public Neo4jOptions Neo4j { set; get; } = new();
    public PsqlOptions Psql { set; get; } = new();

    public int DefaultConnectionLimit { set; get; } = 50;

    public Options()
    {
        TimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}
