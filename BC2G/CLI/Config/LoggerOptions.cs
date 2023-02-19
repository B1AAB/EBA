namespace BC2G.CLI.Config;

public class LoggerOptions
{
    public string RepoName { init; get; } = "events_log";

    public string MessageTemplate { init; get; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
}
