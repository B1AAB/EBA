using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System.Collections.Concurrent;

namespace BC2G.Logging
{
    public class Logger : IDisposable
    {
        public string MaxLogFileSize { get; } = "2GB";

        private readonly ILog log;
        private readonly string _name;
        private readonly string _repository;
        private readonly ChainTraverseProgressBar _progressBar = new();

        private readonly MovingAverage _runtimeMovingAverage;

        private bool disposed = false;


        private int _addedLines = 0;

        private string[] _messages;

        public int CursorTop { get; set; }

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
            Log($"Export Directory: {exportPath}", ConsoleColor.DarkGray);
        }

        public void Log(string message, ConsoleColor color = ConsoleColor.Black, bool newLine = true)
        {
            if (color != ConsoleColor.Black)
                Console.ForegroundColor = color;
            if (newLine)
                Console.WriteLine(message);
            else
                Console.Write(message);
            Console.ResetColor();
            log.Info(message);
        }

        public Dictionary<int, List<string>> TempGetState()
        {
            return _progressBar.tempRecord;
        }
        public Dictionary<int, List<string>> TempGetMessages()
        {
            return _progressBar.tempMessages;
        }


        public void LogStartProcessingBlock(int blockHeight)
        {
            Console.CursorVisible = false;
            for (int line = CursorTop + _addedLines; line >= CursorTop; line--)
                AsyncConsole.WriteAsync(new string(' ', Console.WindowWidth - 1) + "\r", 0, line);
            _addedLines = 0;

            AsyncConsole.WriteAsync($"\r{blockHeight}\t ({_runtimeMovingAverage.Speed} B/sec)");
        }

        public void LogFinishProcessingBlock(int blockHeight, double runtime)
        {
            _addedLines++;
            _runtimeMovingAverage.Add(runtime);
            AsyncConsole.WriteAsync($"\n  *  Successfully finished processing block in {Math.Round(runtime, 2)} seconds.");
        }

        public void LogStatusProcessingBlock(BlockProcessStatus status, bool started = true, double runtime = 0)
        {
            _addedLines++;
            if (status == BlockProcessStatus.ProcessTransactions && !started)
                AsyncConsole.WriteAsync("\r  └  " + _messages[(byte)status] +
                    "\t... " + $"Done ({Math.Round(runtime, 2)} sec)");
            else if (started)
                AsyncConsole.WriteAsync(
                    "\n  └  " + _messages[(byte)status] +
                    "\t... ");
            else
                AsyncConsole.WriteAsync($"Done ({Math.Round(runtime, 2)} sec)");
        }

        public void LogTransaction(string msg)
        {
            msg = "\r  └  " + _messages[(byte)BlockProcessStatus.ProcessTransactions] + "\t... " + msg;
            AsyncConsole.WriteAsync(msg);
        }

        public void LogTraverse(int block, double runtime)
        {
            _runtimeMovingAverage.Add(runtime);
            Console.Write($"\r{block}\t{_runtimeMovingAverage.Speed}");
        }

        public void LogTraverse(int height, string status, double runtime=-1)
        {
            if (runtime != -1)
                _runtimeMovingAverage.Add(runtime);

            //msgQueue.Add($"\r{height}\t (Rate: {_runtimeMovingAverage.Speed}B/sec)");
            //Console.Write($"\r{height}\t (Rate: {_runtimeMovingAverage.Speed}B/sec)");
        }


        public void LogTraverse(int threadId, string status, BlockTraverseState state)
        {
            //_runtimeMovingAverage.Add(runtime);
            //Console.Write($"\t{_runtimeMovingAverage.Speed}");
            _progressBar.Update(threadId, status, state, CursorTop + 1);
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
