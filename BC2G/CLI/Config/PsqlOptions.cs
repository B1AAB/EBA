namespace BC2G.CLI.Config;

public class PsqlOptions
{
    public string Host { init; get; } = "localhost";
    public string Database { init; get; } = "BC2G";
    public string Username { init; get; } = "postgres";
    public string Password { init; get; } = "password";
}
