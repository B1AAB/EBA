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
        public int MovingAvgWindowSize { set; get; } = 10;

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
            /* 01 */ (1, 8, "└  Getting block hash      ...                          "),
            /* 02 */ (1, 8, "└  Getting block hash      ... Done ({0:f2} sec)        "),
            /* 03 */ (1, 8, "└  Getting block hash      ... Cancelled                "),
            /* 04 */ (2, 8, "└  Getting block           ...                          "),
            /* 05 */ (2, 8, "└  Getting block           ... Done ({0:f2} sec)        "),
            /* 06 */ (2, 8, "└  Getting block           ... Cancelled                "),
            /* 07 */ (3, 8, "└  Processing Transactions ...                          "),
            /* 08 */ (3, 8, "└  Processing Transactions ... {0}                      "),
            /* 09 */ (3, 8, "└  Processing Transactions ... Done ({0:f2} sec)        "),
            /* 10 */ (3, 8, "└  Processing Transactions ... Cancelled                "),
            /* 11 */ (4, 8, "└  Serializing             ...                          "),
            /* 12 */ (4, 8, "└  Serializing             ... Done ({0:f2} sec)        "),
            /* 13 */ (4, 8, "└  Serializing             ... Cancelled                "),
            /* 14 */ (5, 8, "*  Finished successfully in    {0:f2} seconds.          "),
            /* 15 */ (5, 8, "-  Cancelled!                                           "),
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
            _runtimeMovingAverage = new MovingAverage(MovingAvgWindowSize);

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
            AsyncConsole.WriteLine("");
        }

        public void Log(string message, bool writeLine = true)
        {
            if (writeLine)
                AsyncConsole.WriteLine(message);
            else
                AsyncConsole.Write(message);
            
            log.Info(message);
        }

        public void Log(string message, bool writeLine, ConsoleColor color)
        {
            if (writeLine)
                AsyncConsole.WriteLine(message, color);
            else
                AsyncConsole.Write(message, color);

            log.Info(message);
        }

        
        public void LogStartProcessingBlock(int blockHeight)
        {
            //AsyncConsole.EraseBlockProgressReport();

            int completed = blockHeight - _from;
            double percentage = (completed / (double)(_to - _from)) * 100.0;
            var (cursorTopOffset, cursorLeft, template) = _messages[(int)BPS.StartBlock];

            var msg = string.Format(
                template, blockHeight, completed,
                _to - _from, percentage, _runtimeMovingAverage.Speed);
            AsyncConsole.WriteLine(msg, cursorTopOffset, cursorLeft, ConsoleColor.Cyan);
        }

        /*
        public void LogFinishProcessingBlock(double runtime)
        {
            _runtimeMovingAverage.Add(runtime);
           // var (cursorTopOffset, cursorLeft, template) = _messages[(byte)BPS.Successful];
           // var msg = string.Format(template, runtime);
            //AsyncConsole.WriteLine(msg, cursorTopOffset, cursorLeft, ConsoleColor.DarkCyan);
            //log.Info(msg);
        }*/

        /*
        public static void LogFinishTraverse(bool cancelled)
        {
            var offset = _firstLineAfterBlockProgress;
            if (cancelled)
                offset++;
            AsyncConsole.MoveCursorTo(0, offset);
        }*/

        public void LogBlockProcessStatus(BPS status, double runtime = 0)
        {
            var (cursorTopOffset, cursorLeft, template) = _messages[(byte)status];
            var msg = string.Format(template, runtime);
            AsyncConsole.WriteLine(msg, cursorTopOffset, cursorLeft, ConsoleColor.DarkCyan);
            log.Info(msg);
        }

        public void LogTransaction(string msg)
        {
            var (cursorTopOffset, cursorLeft, template) = _messages[(byte)BPS.ProcessTransactionsStatus];
            msg = string.Format(template, msg);

            AsyncConsole.WriteLine(msg, cursorTopOffset, cursorLeft, ConsoleColor.DarkCyan);
            log.Info(msg);
        }

        public void LogCancelleing()
        {
            var (cursorTopOffset, cursorLeft, template) = _messages[(byte)BPS.Cancelling];
            AsyncConsole.MoveCursorToOffset(cursorLeft,  cursorTopOffset);
            AsyncConsole.WriteLine(template, ConsoleColor.Yellow);
            log.Info(template);
        }

        public static void LogCancelledTasks(BPS[] tasks)
        {
            foreach(var task in tasks)
            {
                var (cursorTopOffset, cursorLeft, template) = _messages[(byte)task];
                AsyncConsole.WriteLine(template, cursorTopOffset, cursorLeft, ConsoleColor.DarkGray);
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
            AsyncConsole.WriteErrorLine($"Error: {message}");
            // TODO:
            // Console.WriteLine(HintHelpMessage);
            // Console.WriteLine(_cannotContinue);
        }

        public void LogWarning(string message)
        {
            log.Warn(message);
            LogWarningStatic(message);
        }

        public static void LogWarningStatic(string message)
        {
            AsyncConsole.WriteLine($"Warning: {message}", ConsoleColor.DarkMagenta);
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
