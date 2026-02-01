using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Core
{
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
    }

    public sealed class Logger
    {
        public static bool LogToConsole = false;
        public static bool LogErrorsToConsole = true;
        public static bool HasErrors = false;
        public static bool HasWarnings = false;
        public static int BatchInterval = 1000;

        public readonly string LogId;
        public static FileInfo TargetLogFile { get; private set; }
        public static DirectoryInfo TargetDirectory { get { return TargetLogFile?.Directory; } }

        public static bool Listening { get; private set; }
        private static readonly Timer Timer = new Timer(Tick);
        private static readonly StringBuilder LogQueue = new StringBuilder();

        public static EventHandler<LogMessageInfo> LogMessageAdded;

        public Logger(Type t) : this(t.Name)
        {
        }

        public Logger(string logId)
        {
            LogId = logId;
        }

        public static void Start(FileInfo targetLogFile = null, bool overwrite = true, string logDirectory = null)
        {
            if (Listening)
                return;

            if (targetLogFile != null)
                TargetLogFile = targetLogFile;
            else
            {
                var assembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
                string logDir = string.IsNullOrEmpty(logDirectory) ? assembly.DirectoryName : logDirectory;
                TargetLogFile = new FileInfo(Path.Combine(logDir, Path.GetFileNameWithoutExtension(assembly.Name) + ".log"));
            }

            if (overwrite && File.Exists(TargetLogFile.FullName))
                File.Delete(TargetLogFile.FullName);

            Listening = true;
            VerifyTargetDirectory();
            Timer.Change(BatchInterval, Timeout.Infinite); // A one-off tick event that is reset every time.

            Log("Log started.");
        }

        public static void Shutdown()
        {
            if (!Listening)
                return;

            Log("Log stopped.");
            Listening = false;
            Timer.Dispose();
            Tick(null); // Flush.
        }

        private static void VerifyTargetDirectory()
        {
            if (TargetDirectory == null)
                throw new DirectoryNotFoundException("Target logging directory not found.");

            TargetDirectory.Refresh();
            if (!TargetDirectory.Exists)
                TargetDirectory.Create();
        }

        private static void Tick(object state)
        {
            try
            {
                string logMessage;
                lock (LogQueue)
                {
                    logMessage = LogQueue.ToString();
                    LogQueue.Length = 0;
                }

                if (string.IsNullOrEmpty(logMessage))
                    return;

                VerifyTargetDirectory(); // File may be deleted after initialization.
                File.AppendAllText(TargetLogFile.FullName, logMessage);
            }
            finally
            {
                if (Listening)
                    Timer.Change(BatchInterval, Timeout.Infinite); // Reset timer for next tick.
            }
        }

        [Conditional("DEBUG")]
        public void Debug(string message)
        {
            Log($"<{Assembly.GetCallingAssembly().GetName().Name}> " + message, LogLevel.Debug);
        }

        public void Info(string message)
        {
            Log($"<{Assembly.GetCallingAssembly().GetName().Name}> " + message, LogLevel.Information);
        }

        public void Warning(string message)
        {
            HasWarnings = true;
            Log($"<{Assembly.GetCallingAssembly().GetName().Name}> " + message, LogLevel.Warning);
        }

        public void Error(string message, Exception exception = null)
        {
            HasErrors = true;
            var list = new List<Exception>();

            while (exception != null)
            {
                list.Add(exception);
                exception = exception.InnerException;
            }
            //if (list.Count > 0)
            //    exception = list[list.Count - 1];

            Log($"<{Assembly.GetCallingAssembly().GetName().Name}> " + message, LogLevel.Error, list.LastOrDefault());


            /*
            do
            {
                Log($"<{Assembly.GetCallingAssembly().GetName().Name}> " + message, LogLevel.Error, exception);
                exception = exception?.InnerException;
            } while (exception != null);
            */
        }

        public void Log(string message, LogLevel level = LogLevel.Information, Exception exception = null)
        {
            Log(message, LogId, level, exception);
        }

        public static void Log(string message, string logger = null, LogLevel level = LogLevel.Information, Exception exception = null)
        {
            if (!Listening)
                throw new Exception("Logging has not been started.");

            if (exception != null)
                message += $"\r\n{exception.Message}\r\n{exception.StackTrace}";

            var info = new LogMessageInfo(level, logger, message);
            var msg = info.ToString();

            lock (LogQueue)
            {
                LogQueue.AppendLine(msg);
                if (LogToConsole || (LogErrorsToConsole && level >= LogLevel.Warning))
                    Console.WriteLine(msg);
            }

            var evnt = LogMessageAdded;
            evnt?.Invoke(null, info); // Block caller. evnt?.Invoke(this, info);
        }
    }

    public sealed class LogMessageInfo : EventArgs
    {
        public readonly DateTime Timestamp;
        public readonly string ThreadId;
        public readonly LogLevel LogLevel;
        public readonly string LogId;
        public readonly string Message;

        public LogMessageInfo(LogLevel level, string logId, string message)
        {
            Timestamp = DateTime.UtcNow;
            var thread = Thread.CurrentThread;
            ThreadId = string.IsNullOrEmpty(thread.Name) ? thread.ManagedThreadId.ToString() : thread.Name;
            LogLevel = level;
            LogId = logId;
            Message = message;
        }

        public bool IsInformation => LogLevel == LogLevel.Information;
        public bool IsDebug => LogLevel == LogLevel.Debug;
        public bool IsWarning => LogLevel == LogLevel.Warning;
        public bool IsError => LogLevel == LogLevel.Error;

        public override string ToString()
        {
            var logId = string.IsNullOrEmpty(LogId) ? "" : $"[{LogId}]";
            var level = IsInformation ? "" : $"[{LogLevel}]";
            return $"[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}][{ThreadId}]{logId}{level} {Message}";
        }
    }
}
