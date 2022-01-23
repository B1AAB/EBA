using BC2G;
using BC2G.CLI;
using BC2G.Logging;

var tokenSource = new CancellationTokenSource();
var cancellationToken = tokenSource.Token;

var client = new HttpClient();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.UserAgent.Clear();
client.DefaultRequestHeaders.Add("User-Agent", "BitcoinAgent");

try
{
    var cliOptions = new CommandLineOptions();
    var options = cliOptions.Parse(args, out bool helpOrVersionIsDisplayed);
    if (helpOrVersionIsDisplayed)
        return;

    Orchestrator orchestrator;
    try
    {
        orchestrator = new Orchestrator(
            options, client, cliOptions.StatusFilename);

        AppDomain.CurrentDomain.ProcessExit +=
            (sender, e) => ProcessExit(
                sender, e, tokenSource, orchestrator.Logger);

        Console.CancelKeyPress += new ConsoleCancelEventHandler(
            (sender, e) => CancelKeyPressHandler(
                sender, e, tokenSource, orchestrator.Logger));
    }
    catch
    {
        Environment.Exit(1);
        return;
    }

    var success = await orchestrator.RunAsync(cancellationToken);
    if (!success)
        Environment.Exit(1);
}
catch (Exception e)
{
    Environment.Exit(1);
    Console.Error.WriteLine(e.Message);
}

static void ProcessExit(
    object? sender, 
    EventArgs e,
    CancellationTokenSource tokenSource,
    Logger logger)
{
    if (!tokenSource.IsCancellationRequested)
        tokenSource.Cancel();

    logger.Log("Exiting application.");
}

static void CancelKeyPressHandler(
    object? sender, 
    ConsoleCancelEventArgs e,
    CancellationTokenSource tokenSource,
    Logger logger)
{
    tokenSource.Cancel();
    e.Cancel = true;
    logger.LogCancelleing();
}
