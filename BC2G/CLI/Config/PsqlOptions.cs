namespace BC2G.CLI.Config;

public class PsqlOptions
{
    public string Host { set; get; } = "localhost";
    public string Database { set; get; } = "BC2G";
    public string Username { set; get; } = "postgres";
    public string Password { set; get; } = "password";
}
