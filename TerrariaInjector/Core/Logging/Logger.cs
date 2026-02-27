using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace TerrariaInjector.Core.Logging;

public sealed class Logger : ILogger
{
    private LoggerImpl _logger;
    public bool Started { get; private set; }
    public bool HasErrors { get; private set; }
    public DateTime StartTime { get; private set; }
    public LoggerOptions Options { get; }
    
    public void Dispose()
    {
        Started = false;
    }

    internal Logger(LoggerImpl logger, LoggerOptions options, EventHandler<LogInfoArgs> logMessageAdded)
    {
        _logger = logger;
        Started = true;
        Options = options;
        StartTime = DateTime.Now;
        FormatFileName();
        if (logMessageAdded != null)
        {
            LoggerImpl.LogMessageAdded += logMessageAdded;
        }

        if (options.OverwriteOldLog == false)
        {
            BackupOldLog();
            
        }
    }

    private void BackupOldLog()
    {
        if (Options.LogFile.Exists)
        {
            var oldFile = Options.LogFile;
            var fileLoc = $"{oldFile.FullName}.{StartTime.ToString(Constants.FILE_DATE_FORMAT)}.old";
            oldFile = new FileInfo(fileLoc);
            if (oldFile.Exists)
            {
                var rngIdNum = new Random(StartTime.Millisecond).Next();
                oldFile = new FileInfo(oldFile.FullName.Replace(".old", $"{rngIdNum}.old"));
            }
            File.Move(Options.LogFile.FullName, oldFile.FullName);
        }
    }

    private void FormatFileName()
    {
        var file = Options.LogFile.FullName;
        // %date%
        var newFile = file.Replace("%date%", StartTime.ToString(Constants.FILE_DATE_FORMAT));
        Options.LogFile = new FileInfo(newFile);
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