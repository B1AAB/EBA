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

            var success = false;

            try
            {
                var cliOptions = new CommandLineOptions();
                var options = cliOptions.Parse(args, out bool helpOrVersionIsDisplayed);
                if (helpOrVersionIsDisplayed)
                    return;

                var orchestrator = new Orchestrator(
                    options, client, cliOptions.StatusFilename);

                Console.CancelKeyPress += new ConsoleCancelEventHandler(
                    (sender, e) => CancelKeyPressHandler(
                        e, _tokenSource, orchestrator.Logger));

                success = orchestrator.RunAsync(cancellationToken).Result;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            finally
            {
                AsyncConsole.WaitUntilBufferEmpty();
                Console.CursorVisible = true;
                Environment.Exit(success ? 0 : 1);
            }
        }

        private static bool ConsoleEventCallback(
            EventType eventType)
        {
            // This method will be called when the application 
            // is being exited abruptly, e.g., terminal closing, 
            // or the host OS restrating/shutting-down. 
            // HOWEVER, NOTE THAT THIS METHOD NEEDS TO WRAP-UP
            // IN __5__ SECONDS (depending on the host OS), 
            // OR IT WILL BE FORCE-TERMINATED BY THE HOST. HENCE,
            // AFTER THE CANCEL FLAG, ALL THE RUNNING PROCESSED
            // NEED TO SAFELY RETURN QUICKEST POSSIBLE. 
            // THEREFORE, LIMIT THE SCOPE TO CLOSE THE MOST
            // CRITICAL HANDLES.

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
