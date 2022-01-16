using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Logging
{
    public class ChainTraverseProgressBar
    {
        private readonly Queue<int> _availableRows = new();
        private readonly Dictionary<int, int> _idRowMapping = new();
        private object _locker = new object();

        private int startRow = 1;

        public Dictionary<int, List<string>> tempRecord = new();
        public Dictionary<int, List<string>> tempMessages = new();



        private ConcurrentDictionary<int, int> _threadId2ConsoleLine = new();

        private ConcurrentDictionary<int, int> _threads = new();

        private ConcurrentQueue<int> availableRows = new ConcurrentQueue<int>();

        //private object _locker = new object();

        public void Update(int threadId, string status)
        {
            /*
            int currentLineCursor = 0;//Console.CursorTop;
            Console.SetCursorPosition(0, 0);// Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);// currentLineCursor);
            */
            //Console.CursorTop = 0;
            //Console.CursorLeft = 0;
            //Console.Write(new string(' ', Console.WindowWidth));
            //Console.CursorLeft = 0;
            //Console.CursorTop = 0;

            /*
            if (status == "end")
                _threads.TryRemove(threadId, out int removedValue);
            else
                _threads.GetOrAdd(threadId, 0);

            lock (_locker)
            {
                Console.CursorLeft = 0;
                Console.CursorTop = 0;
                Console.CursorVisible = false;

                var x = new string('*', _threads.Count*3);
                var y = new string(' ', Console.WindowWidth - x.Length - 20);
                Console.Write($"\r{x + y}");
                Console.CursorLeft = 0;
                Console.CursorTop = 0;
            }*/
            //return;



            int r = -1;

            if (status == "end")
            {
                _threadId2ConsoleLine.TryRemove(threadId, out int removed);
                availableRows.Enqueue(removed);
                lock (_locker)
                {
                    Console.CursorTop = removed;
                    Console.CursorLeft = 0;
                    Console.WriteLine(new String(' ', Console.WindowWidth - 20));
                }
            }
            else
            {
                while (!availableRows.TryDequeue(out r))
                    availableRows.Enqueue(availableRows.Count);
                _threadId2ConsoleLine.TryAdd(threadId, r);
            }

            if (r == -1)
                _threadId2ConsoleLine.TryGetValue(threadId, out r);

            //var row = _threadId2ConsoleLine.GetOrAdd(threadId, _threadId2ConsoleLine.Count);
            lock (_locker)
            {
                Console.CursorVisible = false;
                Console.CursorTop = r;
                Console.CursorLeft = 0;
                Console.WriteLine(status);
            }
        }

        public void Update(int id, string message, BlockTraverseState state)
        {
            lock (_locker)
            {
                if (_idRowMapping.TryGetValue(id, out int row))
                {
                    if (state == BlockTraverseState.Succeeded)
                    {
                        _idRowMapping.Remove(id);
                        _availableRows.Enqueue(row);
                    }
                }
                else
                {
                    if (!_availableRows.TryDequeue(out row))
                        row = _idRowMapping.Count + startRow;

                    _idRowMapping.Add(id, row);
                }

                if(state == BlockTraverseState.Aborted)
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.CursorVisible = false;
                Console.CursorLeft = 0;
                Console.CursorTop = row;
                Console.Write(message + new string(' ', Console.WindowWidth - message.Length));
                Console.ResetColor();
            }
        }
    }
}
