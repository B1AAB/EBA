namespace BC2G;

internal class Program
{
    /* TODO: how this can be made cross-platform?!
    // See the following SO topics handeling resource
    // clean-up on console exits:
    // - https://stackoverflow.com/a/474743/947889
    // - https://stackoverflow.com/a/4647168/947889
    // Platform Invoke
    [DllImport("Kernel32")]
    private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
    private delegate bool EventHandler(EventType eventType);
    private static EventHandler _handler;
    */
    enum EventType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    private static readonly CancellationTokenSource _tokenSource = new();

    static async Task<int> Main(string[] args)
    {
        var cancellationToken = _tokenSource.Token;

        /*
        _handler += new EventHandler(ConsoleEventCallback);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            SetConsoleCtrlHandler(_handler, true);*/

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

    private static bool ConsoleEventCallback(
        EventType eventType)
    {
        // ***************************************************
        // This method will be called when the application 
        // is being exited abruptly, e.g., terminal closing, 
        // or the host OS restrating/shutting-down. 
        // HOWEVER, NOTE THAT THIS METHOD NEEDS TO WRAP-UP
        // IN __5__ SECONDS (depending on the host OS), 
        // OR IT WILL BE FORCE-TERMINATED BY THE HOST. HENCE,
        // AFTER THE CANCEL FLAG, ALL THE RUNNING PROCESSES
        // NEED TO SAFELY RETURN QUICKEST POSSIBLE. 
        // THEREFORE, LIMIT THE SCOPE TO CLOSING THE MOST
        // CRITICAL HANDLES.
        // ***************************************************

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
}
