using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace BC2G.Logging
{
    public class Logger : IDisposable
    {
        public string MaxLogFileSize { get; } = "2GB";

        private readonly ILog log;
        private readonly string _name;
        private readonly string _repository;

        private int _from;
        private int _to;

        private readonly MovingAverage _runtimeMovingAverage;

        private bool disposed = false;

        // The order is dependent on the BlockProcessStatus.
        private static readonly (int cursorTopOffset, int cursorLeft, string template)[] _messages = new[]
        {
            /* 00 */ (0, 4, "In progress: {0:n0}\tCompleted: {1:n0}/{2:n0} ({3:f1}%)\tRate: {4} B/sec"),
            /* 01 */ (1, 8, "└  Getting block hash      ... "),
            /* 02 */ (1, 8, "└  Getting block hash      ... Done ({0:f2} sec)"),
            /* 03 */ (1, 8, "└  Getting block hash      ... Cancelled"),
            /* 04 */ (2, 8, "└  Getting block           ... "),
            /* 05 */ (2, 8, "└  Getting block           ... Done ({0:f2} sec)"),
            /* 06 */ (2, 8, "└  Getting block           ... Cancelled"),
            /* 07 */ (3, 8, "└  Processing Transactions ... "),
            /* 08 */ (3, 8, "└  Processing Transactions ... {0}"),
            /* 09 */ (3, 8, "└  Processing Transactions ... Done ({0:f2} sec)       "),
            /* 10 */ (3, 8, "└  Processing Transactions ... Cancelled               "),
            /* 11 */ (4, 8, "└  Serializing             ... "),
            /* 12 */ (4, 8, "└  Serializing             ... Done ({0:f2} sec)"),
            /* 13 */ (4, 8, "└  Serializing             ... Cancelled"),
            /* 14 */ (5, 8, "*  Finished successfully in    {0:f2} seconds."),
            /* 15 */ (5, 8, "-  Cancelled!"),
            /* 16 */ (7, 0, "Cancelling ... do not turn off your computer.")
        };
        private static readonly int _blockProgressLinesCount = 5;
        private static readonly int _firstLineAfterBlockProgress = 10;

        public Logger(
            string logFilename, string repository,
            string name, string exportPath,
            string maxLogFileSize)
        {
            MaxLogFileSize = maxLogFileSize;
            AsyncConsole.BlockProgressLinesCount = _blockProgressLinesCount;
            _runtimeMovingAverage = new MovingAverage(10);

            _name = name;
            _repository = repository;
            LogManager.CreateRepository(_repository);
            var hierarchy = (Hierarchy)LogManager.GetRepository(_repository);

            var patternLayout = new PatternLayout
            {
                ConversionPattern = "%date\t[%thread]\t%-5level\t%message%newline"
            };
            patternLayout.ActivateOptions();

            var roller = new RollingFileAppender
            {
                AppendToFile = true,
                File = logFilename,
                Layout = patternLayout,
                MaxSizeRollBackups = 5,
                MaximumFileSize = MaxLogFileSize,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                StaticLogFileName = true
            };
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            var memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = Level.Info;
            hierarchy.Configured = true;
            log = LogManager.GetLogger(_repository, _name);

            log.Info("NOTE THAT THE LOG PATTERN IS: <Date> <#Thread> <Level> <Message>");
            Log($"Export Directory: {exportPath}", true, ConsoleColor.DarkGray);
        }

        public void InitBlocksTraverseLog(int from, int to)
        {
            _from = from;
            _to = to;
            AsyncConsole.WriteLineAsync("");
        }

        public void Log(string message, bool writeLine = true)
        {
            if (writeLine)
                AsyncConsole.WriteLineAsync(message);
            else
                AsyncConsole.WriteAsync(message);
            
            log.Info(message);
        }

        public void Log(string message, bool writeLine, ConsoleColor color)
        {
            if (writeLine)
                AsyncConsole.WriteLineAsync(message, color);
            else
                AsyncConsole.WriteAsync(message, color);

            log.Info(message);
        }

        public void LogStartProcessingBlock(int blockHeight)
        {
            //AsyncConsole.EraseToBookmarkedLine();
            AsyncConsole.EraseBlockProgressReport();

            int completed = blockHeight - _from;
            double percentage = (completed / (double)(_to - _from)) * 100.0;
            var (cursorTopOffset, cursorLeft, template) = _messages[(int)BPS.StartBlock];

            var msg = string.Format(template, blockHeight, completed, _to - _from, percentage, _runtimeMovingAverage.Speed);
            AsyncConsole.WriteLineAsync(msg, cursorTopOffset, cursorLeft, ConsoleColor.Cyan);

            /*
            AsyncConsole.WriteLineAsync(
                $"\r\tIn progress: {blockHeight:n0}" +
                $"\tCompleted: {completed:n0}/{_to - _from:n0} ({percentage:f1}%)" +
                $"\tRate: {_runtimeMovingAverage.Speed} B/sec", 
                ConsoleColor.Cyan);*/
        }

        public void LogFinishProcessingBlock(int blockHeight, double runtime)
        {
            _runtimeMovingAverage.Add(runtime);
            var (cursorTopOffset, cursorLeft, template) = _messages[(byte)BPS.Successful];
            var msg2 = string.Format(template, runtime);
            AsyncConsole.WriteLineAsync(msg2, cursorTopOffset, cursorLeft, ConsoleColor.DarkCyan);
            log.Info(msg2);
            /*
            var (message, offset) = _messages2[(int)BlockProcessStatus.Successful];
            var msg = $"\t  *  {message} " +
                $"block in {Math.Round(runtime, 2)} seconds.";
            AsyncConsole.WriteLineAsync(msg, offset, ConsoleColor.DarkCyan);*/
            //log.Info(msg);
        }

        public static void LogFinishTraverse(bool cancelled)
        {
            var offset = _firstLineAfterBlockProgress;
            if (cancelled)
                offset++;
            AsyncConsole.MoveCursorTo(0, offset);
        }

        public void LogBlockProcessStatus(BPS status, bool started = true, double runtime = 0)
        {
            string msg;
            //var (message, offset) = _messages2[(byte)status];
            var (cursorTopOffset, cursorLeft, template) = _messages[(byte)status];
            if (status == BPS.ProcessTransactions && !started)
            {
                //msg = "\r\t  └  " + message + "\t... " + $"Done ({Math.Round(runtime, 2)} sec)";
                msg = string.Format(template, Math.Round(runtime, 2));
                //AsyncConsole.WriteLineAsync(msg, color: ConsoleColor.DarkCyan);
            }
            else if (started)
            {
                msg = template;
                //msg = "\t  └  " + message + "\t... ";
                //AsyncConsole.WriteAsync(msg, color: ConsoleColor.DarkCyan);
            }
            else
            {
                msg = string.Format(template, Math.Round(runtime, 2));

                //msg = $"Done ({Math.Round(runtime, 2)} sec)";
                //AsyncConsole.WriteLineAsync(msg, color: ConsoleColor.DarkCyan);
            }

            //AsyncConsole.WriteLineAsync(msg, offset, ConsoleColor.DarkCyan);
            AsyncConsole.WriteLineAsync(msg, cursorTopOffset, cursorLeft, ConsoleColor.DarkCyan);

            log.Info(msg);
        }

        public void LogTransaction(string msg)
        {
            //var (message, offset) = _messages2[(byte)BlockProcessStatus.ProcessTransactions];
            var (cursorTopOffset, cursorLeft, template) = _messages[(byte)BPS.ProcessTransactionsStatus];
            msg = string.Format(template, msg);

            AsyncConsole.WriteLineAsync(msg, cursorTopOffset, cursorLeft, ConsoleColor.DarkCyan);

            //msg = "\r\t  └  " + message + "\t... " + msg;
            //AsyncConsole.WriteAsync(msg, ConsoleColor.DarkCyan, offset);
            log.Info(msg);
        }

        public void LogCancelleing()
        {
            var (cursorTopOffset, cursorLeft, template) = _messages[(byte)BPS.Cancelling];
            //var msg = "Cancelling ... do not turn off your computer.";
            //AsyncConsole.WriteLineAsyncAfterAddedLines(msg, ConsoleColor.Yellow);
            AsyncConsole.MoveCursorToOffset(cursorLeft,  cursorTopOffset);
            AsyncConsole.WriteLineAsync(template, ConsoleColor.Yellow);
            //log.Info(msg);
            log.Info(template);
        }

        public static void LogCancelledTasks(BPS[] tasks)
        {
            foreach(var task in tasks)
            {
                var (cursorTopOffset, cursorLeft, template) = _messages[(byte)task];
                AsyncConsole.WriteLineAsync(template, cursorTopOffset, cursorLeft, ConsoleColor.DarkGray);
            }
        }

        public void LogException(Exception e)
        {
            LogException(e.Message);
        }

        public void LogException(string message)
        {
            LogExceptionStatic(message);
            log.Error(message);

            // TODO:
            /*
            log.Info(_linkToDocumentation);
            log.Info(_linkToIssuesPage);
            log.Info(HintHelpMessage);
            log.Info(_cannotContinue);*/
        }

        public static void LogExceptionStatic(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(string.Format("Error: {0}", message));
            Console.ForegroundColor = ConsoleColor.Yellow;
            // TODO:
            // Console.WriteLine(HintHelpMessage);
            Console.ResetColor();
            // TODO:
            // Console.WriteLine(_cannotContinue);
        }

        public void LogWarning(string message)
        {
            log.Warn(message);
            LogWarningStatic(message);
        }

        public static void LogWarningStatic(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Warning: {message}");
            Console.ResetColor();
        }

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    LogManager.Flush(5000);

                    var l = (log4net.Repository.Hierarchy.Logger)LogManager
                        .GetLogger(_repository, _name).Logger;
                    l.RemoveAllAppenders();

                    LogManager.GetLogger(_repository, _name)
                        .Logger.Repository.Shutdown();
                }

                disposed = true;
            }
        }
    }
}
