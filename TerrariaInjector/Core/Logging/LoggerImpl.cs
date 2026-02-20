using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace TerrariaInjector.Core.Logging
{
    // TODO: Change to actual implementation.
    internal sealed class LoggerImpl : ILogger
    {
        public static ILogger Instance { get; private set; }
        
        public bool Started { get; private set; }
        public bool HasErrors { get; private set; }
        public LoggerOptions Options { get; }

        private static readonly Timer Timer = new Timer(Tick);
        private static readonly ConcurrentQueue<LogMessage> LogQueue = new ConcurrentQueue<LogMessage>();
        

        public static EventHandler<LogMessage> LogMessageAdded;
        

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
        
        private LoggerImpl(LoggerOptions options)
        {
            Instance = this;
            Options = options;
            Started = true;
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
            
            var logMessage = new LogMessage(level, options.LoggerName, message, exp);
            LogQueue.Enqueue(logMessage);
            if (options.LogToConsole && (options.LogErrorsToConsole || level >= LogLevel.Warning))
            {
                Console.WriteLine(logMessage);
            }
            
            var _event = LogMessageAdded;
            _event?.Invoke(null, logMessage);
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
            if (LogQueue.TryDequeue(out var message))
            {
                if (message.IsEmpty)
                {
                    return;   
                }
                // maybe verify file and dir.
                File.AppendAllText(Instance.Options.LogFile.FullName, message.ToString(true));
            }

            if (Instance.Started)
            {
                Timer.Change(Instance.Options.BatchInterval, Timeout.Infinite);
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