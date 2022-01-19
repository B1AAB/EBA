using System.Collections.Concurrent;

namespace BC2G.Logging
{
    public static class AsyncConsole
    {
        private static readonly BlockingCollection<(string, int?, int?)> _queue = new();

        static AsyncConsole()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    (string value, int? left, int? top) = _queue.Take();
                    if (left != null && top != null)
                        Console.SetCursorPosition((int)left, (int)top);
                    Console.Write(value);
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
            int? cursorPositionTop = null)
        {
            _queue.Add((value, cursorPositionLeft, cursorPositionTop));
        }
    }
}
