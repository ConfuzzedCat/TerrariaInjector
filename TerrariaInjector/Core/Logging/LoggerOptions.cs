using System.IO;
using System.Runtime.CompilerServices;

namespace TerrariaInjector.Core.Logging;

public class LoggerOptions
{
    public string LoggerName { get; set; } = string.Empty;
    public LogLevel DefaultLogLevel { get; set; } = LogLevel.Debug;
    public bool LogToConsole { get; set; } = true;
    public bool LogErrorsToConsole { get; set; } = true;
    public int BatchInterval { get; set; } = 1000;
    public FileInfo LogFile { get; set; } = new(Constants.DEFAULT_LOG_FILE);
    public bool OverwriteOldLog { get; set; } = true;


    public static readonly LoggerOptions Default = new();
}