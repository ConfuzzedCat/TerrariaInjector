using System;
using System.Threading;

namespace TerrariaInjector.Core.Logging;

public sealed class LogInfoArgs : EventArgs
{
    public readonly DateTime Timestamp;
    public readonly string ThreadId;
    public readonly LogLevel LogLevel;
    public readonly string LogId;
    public readonly string Message;
    public readonly Exception Exception;
    public readonly LoggerOptions Options;

    public LogInfoArgs(LogLevel level, string logId, FormattableString formattableString, LoggerOptions options, Exception exp = null)
    {
        Timestamp = DateTime.UtcNow;
        var thread = Thread.CurrentThread;
        ThreadId = string.IsNullOrEmpty(thread.Name) ? thread.ManagedThreadId.ToString() : thread.Name;
        LogLevel = level;
        LogId = logId;
        Message = formattableString.ToString();
        Exception = exp;
        Options = options;
    }

    public bool IsInformation => LogLevel == LogLevel.Information;
    public bool IsDebug => LogLevel == LogLevel.Debug;
    public bool IsWarning => LogLevel == LogLevel.Warning;
    public bool IsError => LogLevel == LogLevel.Error;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Message);


    public override string ToString()
    {
        var levelString = LogLevel.ToString().ToUpper().PadRight(5);
        
        var logId = string.IsNullOrEmpty(LogId) ? "" : $"[{LogId}]";
        var level = $"[{levelString}]";
        var exp = Exception != null ? $"\n[{Exception.GetType().Name}] {Exception.Message}\n{Exception.StackTrace}" : "";
        return $"[{Timestamp.ToString(Constants.LOG_DATE_FORMAT)}][{ThreadId}]{level}{logId} {Message}{exp}";
    }

    public string ToString(bool newline)
    {
        var s = newline ? "\n" : string.Empty;

        return ToString() + s;
    }
}