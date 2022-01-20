using System.Collections.Concurrent;

namespace BC2G.Logging
{
    public static class AsyncConsole
    {
        private static readonly BlockingCollection<
            (string, int?, int?, ConsoleColor?, bool)> _queue = new();

        static AsyncConsole()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    (string value, int? left, int? top, 
                    ConsoleColor? color, bool newLine) = _queue.Take();
                    if (left != null && top != null)
                        Console.SetCursorPosition((int)left, (int)top);
                    if (color != null)
                        Console.ForegroundColor = (ConsoleColor)color;
                    if (newLine)
                        Console.WriteLine(value);
                    else
                        Console.Write(value);
                    if (color != null)
                        Console.ResetColor();
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void WriteAsync(
            string value,
            int? cursorPositionLeft = null,
            int? cursorPositionTop = null,
            ConsoleColor? color = null,
            bool newLine = false)
        {
            _queue.Add((value, cursorPositionLeft, 
                cursorPositionTop, color, newLine));
        }
    }
}
