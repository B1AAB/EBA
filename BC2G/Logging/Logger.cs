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

        private readonly string[] _messages;

        public Logger(
            string logFilename, string repository,
            string name, string exportPath,
            string maxLogFileSize)
        {
            MaxLogFileSize = maxLogFileSize;
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

            _messages = new string[]
            {
                "Cancelling",
                "Getting block hash\t",
                "Getting block\t",
                "Procesing Transactions",
                "Serializing\t"
            };

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
            AsyncConsole.EraseToBookmarkedLine();

            int completed = blockHeight - _from;
            double percentage = (completed / (double)(_to - _from)) * 100.0;
            AsyncConsole.WriteLineAsync(
                $"\r\tIn progress: {blockHeight:n0}" +
                $"\tCompleted: {completed:n0}/{_to - _from:n0} ({percentage:f1}%)" +
                $"\tRate: {_runtimeMovingAverage.Speed} B/sec", 
                ConsoleColor.Cyan);
        }

        public void LogFinishProcessingBlock(int blockHeight, double runtime)
        {
            _runtimeMovingAverage.Add(runtime);
            var msg = $"\t  *  Successfully finished processing " +
                $"block in {Math.Round(runtime, 2)} seconds.";
            AsyncConsole.WriteLineAsync(msg, ConsoleColor.DarkGray);
            log.Info(msg);
        }

        public void LogBlockProcessStatus(BlockProcessStatus status, bool started = true, double runtime = 0)
        {
            string msg;
            if (status == BlockProcessStatus.ProcessTransactions && !started)
            {
                msg = "\r\t  └  " + _messages[(byte)status] + "\t... " + $"Done ({Math.Round(runtime, 2)} sec)";
                AsyncConsole.WriteLineAsync(msg, color: ConsoleColor.DarkCyan);
            }
            else if (started)
            {
                msg = "\t  └  " + _messages[(byte)status] + "\t... ";
                AsyncConsole.WriteAsync(msg, color: ConsoleColor.DarkCyan);
            }
            else
            {
                msg = $"Done ({Math.Round(runtime, 2)} sec)";
                AsyncConsole.WriteLineAsync(msg, color: ConsoleColor.DarkCyan);
            }

            log.Info(msg);
        }

        public void LogTransaction(string msg)
        {
            msg = "\r\t  └  " + _messages[(byte)BlockProcessStatus.ProcessTransactions] + "\t... " + msg;
            AsyncConsole.WriteAsync(msg, color: ConsoleColor.DarkCyan);
            log.Info(msg);
        }

        public void LogCancelleing()
        {
            var msg = "Cancelling ... do not turn off your computer.";
            AsyncConsole.WriteLineAsyncAfterAddedLines(msg, ConsoleColor.Yellow);
            log.Info(msg);
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
