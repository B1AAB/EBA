using BC2G.CLI;
using BC2G.Logging;
using System.Runtime.InteropServices;

namespace BC2G
{
    internal class Program
    {
        // See the following SO topics handeling resource
        // clean-up on console exits:
        // - https://stackoverflow.com/a/474743/947889
        // - https://stackoverflow.com/a/4647168/947889
        // Platform Invoke
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler(EventType eventType);
        private static EventHandler _handler;
        enum EventType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static readonly CancellationTokenSource _tokenSource = new();

        static void Main(string[] args)
        {
            var cancellationToken = _tokenSource.Token;

            _handler += new EventHandler(ConsoleEventCallback);
            SetConsoleCtrlHandler(_handler, true);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.UserAgent.Clear();
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

                    Console.CancelKeyPress += new ConsoleCancelEventHandler(
                        (sender, e) => CancelKeyPressHandler(
                            sender, e, _tokenSource, orchestrator.Logger));
                }
                catch
                {
                    Console.CursorVisible = true;
                    Environment.Exit(1);
                    return;
                }

                var success = orchestrator.RunAsync(cancellationToken).Result;
                Environment.Exit(success ? 0 : 1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.CursorVisible = true;
                Environment.Exit(1);
                return;
            }
        }

        private static bool ConsoleEventCallback(
            EventType eventType)
        {
            // NOTE THAT THIS METHOD NEEDS TO WRAP-UP IN 5 SECONDS, 
            // OR IT WILL BE FORCE-TERMINATED BY HOST.

            switch (eventType)
            {
                case EventType.CTRL_C_EVENT:
                case EventType.CTRL_LOGOFF_EVENT:
                case EventType.CTRL_SHUTDOWN_EVENT:
                case EventType.CTRL_CLOSE_EVENT:
                    _tokenSource.Cancel();
                    break;
                default:
                    return false;
            }
            return true;
        }

        static void CancelKeyPressHandler(
            object? sender,
            ConsoleCancelEventArgs e,
            CancellationTokenSource tokenSource,
            Logger logger)
        {
            tokenSource.Cancel();
            e.Cancel = true;
            logger.LogCancelling();
        }
    }
}
