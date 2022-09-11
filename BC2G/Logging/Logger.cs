/*
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

        public ConsoleLoggingInterface ConsoleLoggingInterface { get; }

        private readonly ILog log;
        private readonly string _name;
        private readonly string _repository;

        private BlockTraversalLoggingBase _consoleLogging;

        private bool disposed = false;

        public Logger(
            string logFilename, string repository,
            string name, string exportPath,
            string maxLogFileSize)
        {
            MaxLogFileSize = maxLogFileSize;
            ConsoleLoggingInterface = ConsoleLoggingInterface.Minimal;

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
                StaticLogFileName = true,
                LockingModel = new FileAppender.MinimalLock()
            };
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            hierarchy.Root.Level = Level.Info;
            hierarchy.Configured = true;
            log = LogManager.GetLogger(_repository, _name);

            log.Info("NOTE THAT THE LOG PATTERN IS: <Date> <#Thread> <Level> <Message>");
            Log($"Export Directory: {exportPath}", true, ConsoleColor.DarkGray);
        }
        
        public void InitBlocksTraverse(int from, int to, int blocksToProcess)
        {
            switch(ConsoleLoggingInterface)
            {
                case ConsoleLoggingInterface.Minimal:
                    _consoleLogging = new BlockTraversalLoggingMinimal(from, to, blocksToProcess);
                    break;
                case ConsoleLoggingInterface.Complete:
                    _consoleLogging = new BlockTraversalLoggingComplete(from, to, blocksToProcess);
                    break;
            }
        }

        public void Log(string message)
        {
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
            log.Info(_consoleLogging.Log(blockHeight));
        }

        public void LogFinishProcessingBlock(int height, double runtime)
        {
            log.Info(_consoleLogging.Log(height, runtime));
        }

        public void LogCancelling()
        {
            log.Info(_consoleLogging.LogCancelling());
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
        /*}

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
}*/
