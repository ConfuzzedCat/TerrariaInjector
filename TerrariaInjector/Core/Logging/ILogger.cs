using System;

namespace TerrariaInjector.Core.Logging
{
    public interface ILogger : IDisposable
    {
        bool Started { get; }
        LoggerOptions Options { get; }
        bool HasErrors { get; }
        
        void Log(FormattableString message, Exception exp = null, LogLevel level = LogLevel.Information);
        void LogDebug(FormattableString message);
        void LogInformation(FormattableString message);
        void LogWarning(FormattableString message, Exception exp = null);
        void LogError(FormattableString message, Exception exp);
        void Log(string message, Exception exp = null);
        void LogDebug(string message);
        void LogInformation(string message);
        void LogWarning(string message, Exception exp = null);
        void LogError(string message, Exception exp);
    }
}