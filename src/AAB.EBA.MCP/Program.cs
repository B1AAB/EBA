using Serilog;

namespace AAB.EBA.MCP;

internal class Program
{
    private static readonly CancellationTokenSource _tokenSource = new();

    static async Task<int> Main(string[] args)
    {
        var cancellationToken = _tokenSource.Token;

        var exitCode = 0;

        try
        {
            var logger = Log.Logger;
            var orchestrator = new Orchestrator(cancellationToken);
            Console.CancelKeyPress += (sender, e) =>
            {
                // Flag the cancel token so all listening can exit ASAP.
                _tokenSource.Cancel();

                // Prevents the console from exiting immediately.
                e.Cancel = true;

                logger.Information("Cancelling...");
            };

            exitCode = await orchestrator.InvokeAsync(args);

            if (exitCode == 0)
                logger.Information("All process finished!");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            exitCode = 1;
        }

        // Do not enable the following as it causes issues with building migration scripts.
        //Environment.Exit(exitCode);
        return exitCode;
    }
}









public partial class Monkey
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? Details { get; set; }
    public string? Image { get; set; }
    public int Population { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

[JsonSerializable(typeof(List<Monkey>))]
internal sealed partial class MonkeyContext : JsonSerializerContext
{
}
