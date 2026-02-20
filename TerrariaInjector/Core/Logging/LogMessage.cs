using System;
using System.Threading;

namespace TerrariaInjector.Core.Logging;

public sealed class LogMessage : EventArgs
{
    public readonly DateTime Timestamp;
    public readonly string ThreadId;
    public readonly LogLevel LogLevel;
    public readonly string LogId;
    public readonly string Message;
    public readonly Exception Exception;

    public LogMessage(LogLevel level, string logId, FormattableString formattableString, Exception exp = null)
    {
        Timestamp = DateTime.UtcNow;
        var thread = Thread.CurrentThread;
        ThreadId = string.IsNullOrEmpty(thread.Name) ? thread.ManagedThreadId.ToString() : thread.Name;
        LogLevel = level;
        LogId = logId;
        Message = formattableString.ToString();
        Exception = exp;
    }

    public bool IsInformation => LogLevel == LogLevel.Information;
    public bool IsDebug => LogLevel == LogLevel.Debug;
    public bool IsWarning => LogLevel == LogLevel.Warning;
    public bool IsError => LogLevel == LogLevel.Error;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Message);


    public override string ToString()
    {
        var levelString = string.Empty;
        switch (LogLevel)
        {
            case LogLevel.Debug:
                levelString = "DEBUG";
                break;
            case LogLevel.Information:
                levelString = "INFO ";
                break;
            case LogLevel.Warning:
                levelString = "WARN ";
                break;
            case LogLevel.Error:
                levelString = "ERROR";
                break;
        }
        
        
        var logId = string.IsNullOrEmpty(LogId) ? "" : $"[{LogId}]";
        var level = $"[{levelString}]";
        var exp = Exception != null ? $"\n[{Exception.GetType().Name}] {Exception.Message}\n{Exception.StackTrace}" : "";
        return $"[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}][{ThreadId}]{logId}{level} {Message}{exp}";
    }

    public string ToString(bool newline)
    {
        var s = newline ? "\n" : string.Empty;

        return ToString() + s;
    }
}