namespace BC2G.Logging
{
    public static class AsyncConsole
    {
        public static CancellationToken CancellationToken { set; get; }
        public static int BookmarkedLine { set; get; }
        public static int BlockProgressLinesCount { get; set; }

        private static BlockingCollection<Action> _actions = new();

        /// <summary>
        /// Sets the number of most recent messeges that should 
        /// be flushed to console before the process exits on
        /// cancellelation signal.
        /// </summary>
        private const int _nLastItems = 5;

        static AsyncConsole()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        _actions.Take(CancellationToken)();
                    }
                    catch (OperationCanceledException)
                    {
                        // Note that when the cancellelation signal 
                        // is received, this process is not necessarily
                        // current, there could be a big backlog of messages
                        // to write to console. Therefore, waiting for 
                        // all of them to be sent to console before exiting
                        // may take a considerable amount of time; hence
                        // only the _n_ recent items are sent to console.
                        // Flush latest messages before exiting.
                        var mostRecentActions = _actions.TakeLast(_nLastItems);

                        // Set to a new instance so all the post-cancellation
                        // messages are still queued up for display.
                        _actions = new BlockingCollection<Action>();

                        // Show the last _n_ messeges before the
                        // cancellation signal.
                        foreach (var action in mostRecentActions)
                            action();

                        // Still consuming produced message, hence 
                        // it can display any post cancellation messages.
                        // Note that this loop only ends when the
                        // application exits.
                        while (true)
                            _actions.Take()();
                    }
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void Write(string value, ConsoleColor color, int lineOffset = 1)
        {
            _actions.Add(() =>
            {
                // Write to Block Traversal Progress Line.
                bool w2BTL = false;
                try
                {
                    if (Console.CursorTop != BookmarkedLine + 1)
                        w2BTL = true;
                }
                catch (IOException)
                {
                    // This exception is thrown when tested under
                    // xunit, because xunit does not have a console 
                    // attached. 
                    w2BTL = false;
                }

                if (w2BTL)
                {
                    var (Left, Top) = Console.GetCursorPosition();
                    Console.SetCursorPosition(0, BookmarkedLine + lineOffset);
                    Console.ForegroundColor = color;
                    Console.Write(value);
                    Console.ResetColor();
                    Console.SetCursorPosition(Left, Top);
                }
                else
                {
                    Console.ForegroundColor = color;
                    Console.Write(value);
                    Console.ResetColor();
                }
            });
        }

        public static void WriteLine(string value)
        {
            _actions.Add(() =>
            {
                Console.WriteLine(value);
            });
        }

        public static void WriteLine(string value, ConsoleColor color)
        {
            _actions.Add(() =>
            {
                Console.ForegroundColor = color;
                Console.WriteLine(value);
                Console.ResetColor();
            });
        }

        public static void WriteLines(string[] msgs, ConsoleColor[] colors, int cursorTopOffset = 1)
        {
            _actions.Add(() =>
            {
                var (currentLeft, currentTop) = Console.GetCursorPosition();
                Console.SetCursorPosition(0, BookmarkedLine + cursorTopOffset);
                for (int i = 0; i < msgs.Length; i++)
                {
                    Console.ForegroundColor = colors[i];
                    if (msgs[i].Length - 1 >= Console.WindowWidth)
                    {
                        Console.WriteLine(string.Concat(
                            msgs[i].AsSpan(0, Console.WindowWidth - 6), "...   "));
                    }
                    else
                    {
                        Console.Write(msgs[i]);
                        Console.WriteLine(new string(
                            ' ', Console.WindowWidth - Console.CursorLeft - 1));
                    }
                }
                Console.ResetColor();
                Console.SetCursorPosition(currentLeft, currentTop);
            });
        }

        public static void WriteErrorLine(string value, ConsoleColor color = ConsoleColor.Red)
        {
            _actions.Add(() =>
            {
                Console.ForegroundColor = color;
                Console.Error.WriteLine(value);
                Console.ResetColor();
            });
        }

        public static void BookmarkCurrentLine()
        {
            _actions.Add(() =>
            {
                // The exception is thrown with the message 'The handle is invalid.'
                // only when running the tests, because Xunit does not have a console.
                try { BookmarkedLine = Console.CursorTop; }
                catch (IOException) { }
            });
        }

        public static void WaitUntilBufferEmpty()
        {
            while (true)
            {
                if (_actions.Count == 0) return;
                Thread.Sleep(500);
            }
        }
    }
}
