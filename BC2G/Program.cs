using BC2G;
using BC2G.CLI;

var tokenSource = new CancellationTokenSource();
var cancellationToken = tokenSource.Token;

AppDomain.CurrentDomain.ProcessExit +=
(sender, e) => ProcessExit(sender, e, tokenSource);

Console.CancelKeyPress += new ConsoleCancelEventHandler(
    (sender, e) => CancelKeyPressHandler(sender, e, tokenSource));

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
    object? sender, EventArgs e,
    CancellationTokenSource tokenSource)
{
    if (!tokenSource.IsCancellationRequested)
        tokenSource.Cancel();
    
    Console.WriteLine("Exiting application.");
}

static void CancelKeyPressHandler(
    object? sender, ConsoleCancelEventArgs e,
    CancellationTokenSource tokenSource)
{
    tokenSource.Cancel();
    e.Cancel = true;
    Console.WriteLine("Cancelling ... do not turn off your computer.");
}
