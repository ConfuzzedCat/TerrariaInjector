using System;
using System.Runtime.CompilerServices;

namespace TerrariaInjector.Core.Logging;

public sealed class Logger : ILogger
{
    private LoggerImpl _logger;
    public bool Started { get; private set; }
    public bool HasErrors { get; private set; }
    public LoggerOptions Options { get; }

    public void Dispose()
    {
        Started = false;
    }

    internal Logger(LoggerImpl logger, LoggerOptions options)
    {
        _logger = logger;
        Started = true;
        Options = options;
    }
    public void Log(FormattableString message, Exception exp = null, LogLevel level = LogLevel.Information)
    {
        if (_logger.Started == false)
        {
            throw new InvalidOperationException("Logger is not started");
        }
        
        _logger.Log(message, exp, Options, level);
    }

    public void LogDebug(FormattableString message)
    {
        _logger.LogDebug(message, Options);
    }

    public void LogInformation(FormattableString message)
    {
        _logger.LogInformation(message, Options);
    }

    public void LogWarning(FormattableString message, Exception exp = null)
    {
        _logger.LogWarning(message, Options, exp);
    }

    public void LogError(FormattableString message, Exception exp)
    {
        HasErrors = true;
        _logger.LogError(message, exp, Options);
    }

    public void Log(string message, Exception exp = null)
    {
        Log(FormattableStringFactory.Create(message), exp, Options.DefaultLogLevel);
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
        LogError(FormattableStringFactory.Create(message), exp);
    }
}