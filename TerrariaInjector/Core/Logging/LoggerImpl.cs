using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace TerrariaInjector.Core.Logging
{
    // TODO: Change to actual implementation.
    public sealed class LoggerImpl : ILogger
    {
        internal static ILogger Instance { get; private set; }
        
        public bool Started { get; private set; }
        public bool HasErrors { get; private set; }
        public DateTime StartTime { get; private set; }
        public LoggerOptions Options { get; }

        private static readonly Timer Timer = new Timer(Tick);
        private static readonly ConcurrentQueue<LogInfoArgs> LogQueue = new ConcurrentQueue<LogInfoArgs>();
        

        public static EventHandler<LogInfoArgs> LogMessageAdded;

        

        internal static void CreateInstance(LoggerOptions options = null)
        {
            if (Instance is { Started: true })
            {
                return;
            }
            
            if (options == null)
            {
                options = LoggerOptions.Default;
            }

            Instance = new LoggerImpl(options);
        }
        
        private LoggerImpl() : this(LoggerOptions.Default)
        {
            
        }
        private LoggerImpl(LoggerOptions options)
        {
            Instance = this;
            Options = options;
            Started = true;
            StartTime = DateTime.Now;
            Log("Logging started.");
        }
        public void Log(string message, Exception exp = null)
        {
            var level = Options.DefaultLogLevel;
            Log(FormattableStringFactory.Create(message), exp, level);
        }
        public void LogDebug(string message)
        {
            LogDebug(FormattableStringFactory.Create(message));
        }

        public void LogInformation(string message)
        {
            LogInformation(FormattableStringFactory.Create(message));
        }

        public void LogWarning(string message, Exception exp = null)
        {
            LogWarning(FormattableStringFactory.Create(message), exp);
        }

        public void LogError(string message, Exception exp)
        {
            LogError(FormattableStringFactory.Create(message), exp, Options);
        }
        public void Log(FormattableString message, Exception exp, LogLevel level)
        {
            Log(message, exp, Options, level);
        }

        public void LogDebug(FormattableString message)
        {
            LogDebug(message, Options);
        }

        public void LogInformation(FormattableString message)
        {
            LogInformation(message, Options);
        }

        public void LogWarning(FormattableString message, Exception exp = null)
        {
            LogWarning(message, Options, exp);
        }

        public void LogError(FormattableString message, Exception exp)
        {
            LogError(message, exp, Options);
        }
        
        public void Log(FormattableString message, Exception exp, LoggerOptions options, LogLevel level)
        {
            if (Started == false)
            {
                throw new InvalidOperationException("Logger is not started");
            }
            
            var logMessage = new LogInfoArgs(level, options.LoggerName, message, options, exp);
            LogQueue.Enqueue(logMessage);
            if (options.LogToConsole && (options.LogErrorsToConsole || level >= LogLevel.Warning))
            {
                WriteToConsole(logMessage);
            }
            
            var _event = LogMessageAdded;
            _event?.Invoke(this, logMessage);
        }

        private static void WriteToConsole(LogInfoArgs logMessage)
        {
            var curColor = Console.ForegroundColor;
            Console.ForegroundColor = GetConsoleColor(logMessage);
            if (logMessage.IsError)
            {
                Console.Error.WriteLine(logMessage);
            }
            else
            {
                Console.WriteLine(logMessage);
            }
            Console.ForegroundColor = curColor;
        }

        private static ConsoleColor GetConsoleColor(LogInfoArgs logInfoArgs)
        {
            switch (logInfoArgs.LogLevel)
            {
                case LogLevel.Debug:
                    return ConsoleColor.Gray;
                case LogLevel.Information:
                    return ConsoleColor.White;
                case LogLevel.Warning:
                    return ConsoleColor.Yellow;
                case LogLevel.Error:
                    return ConsoleColor.Red;
            }
            return Console.ForegroundColor;
        }

        public void LogDebug(FormattableString message, LoggerOptions options)
        {
            Log(message, null, options, LogLevel.Debug);
        }

        public void LogInformation(FormattableString message, LoggerOptions options)
        {
            Log(message, null, options, LogLevel.Information);
        }

        public void LogWarning(FormattableString message, LoggerOptions options, Exception exp = null)
        {
            Log(message, exp, options, LogLevel.Warning);
        }

        public void LogError(FormattableString message, Exception exp, LoggerOptions options)
        {
            HasErrors = true;
            Log(message, exp, options, LogLevel.Error);
        }

        private static void Tick(object state)
        {
            while (LogQueue.IsEmpty == false)
            {
                if (!LogQueue.TryDequeue(out var message)) continue;
                if (message.IsEmpty)
                {
                    return;   
                }

                // maybe verify file and dir.
                if (message.Options != null)
                {
                    WriteToLogFile(message.Options.LogFile, message);
                }
                WriteToLogFile(Instance.Options.LogFile, message);
                
            }

            if (Instance.Started)
            {
                Timer.Change(Instance.Options.BatchInterval, Timeout.Infinite);
            }
        }

        private static void WriteToLogFile(FileInfo file, LogInfoArgs message)
        {
            VerifyLogDir(file);
            File.AppendAllText(file.FullName, message.ToString(true));
        }

        private static void VerifyLogDir(FileInfo fileInfo)
        {
            var dir = fileInfo.Directory;
            if (dir == null || dir.Exists == false)
            {
                if (string.IsNullOrWhiteSpace(fileInfo.DirectoryName))
                {
                    throw new DirectoryNotFoundException("Could not find the specified file directory");
                }
                var dirInfo = new DirectoryInfo(fileInfo.DirectoryName);
                dirInfo.Create();
            }
        }


        public void Dispose()
        {
            // TODO release managed resources here
            if (Started == false)
            {
                return;
            }
            
            Log("Logging stopped.");
            Started = false;
            Timer.Dispose();
            Tick(null);
        }
    }
}