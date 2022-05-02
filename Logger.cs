using System;
using System.IO;
using System.Reflection;

public static class Logger
{
    public static string LogFilePath
    {
        get;
        private set;
    }
    public static int LogLevel
    {
        get;
        set;
    }
    private static bool IsRunning
    {
        get;
        set;
    } = false;
    private static StreamWriter LogWriter
    {
        get;
        set;
    }
    private static string TimeFormat
    {
        get
        {
            return "yyyy-MM-dd HH:mm:ss.fff";
        }
    }
    private static string LogPath = Path.Combine(Directory.GetCurrentDirectory(), "InjectorLogs");
    public static string Start(int level = 0)
    {
        if (!IsRunning)
        {
            LogFilePath = Path.Combine(LogPath, $"log_{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace('\\', '-').Replace(' ', '-').Replace(':', '-')}.txt");
            IsRunning = true;
            LogLevel = level;
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
            if (File.Exists(LogFilePath))
            {
                Warn("Log file already existed!");
                File.Delete(LogFilePath);
                File.Create(LogFilePath).Close();
            }
            else
            {
                File.Create(LogFilePath).Close();
            }
            LogWriter = File.AppendText(LogFilePath);
            Info("Logging started.");
            return LogFilePath;
        }
        else
        {
            return LogFilePath;
        }
    }
    public static void Stop()
    {
        Info("Logging stopped.");
        ForceWrite();
        IsRunning = false;
        LogWriter.Close();
    }

    public static void ForceWrite()
    {
        LogWriter.Flush();
    }

    public static string Debug(string message)
    {
        string logFrom = Assembly.GetCallingAssembly().GetName().Name;
        string time = DateTime.Now.ToString(TimeFormat);
        string v = $"{time} [Debug] [{logFrom}] {message}";
        if (0 >= LogLevel)
        {
            LogWriter.WriteLine(v);
            Console.WriteLine(v);
        }
        return v;
    }

    public static string Info(string message)
    {
        string logFrom = Assembly.GetCallingAssembly().GetName().Name;
        string time = DateTime.Now.ToString(TimeFormat);
        string v = $"{time} [Info ] [{logFrom}] {message}";
        if (1 >= LogLevel)
        {
            LogWriter.WriteLine(v);
            Console.WriteLine(v);
        }
        return v;
    }

    public static string Warn(string message)
    {
        string logFrom = Assembly.GetCallingAssembly().GetName().Name;
        string time = DateTime.Now.ToString(TimeFormat);
        string v = $"{time} [Warn ] [{logFrom}] {message}";
        if (2 >= LogLevel)
        {
            LogWriter.WriteLine(v);
            Console.WriteLine(v);
        }
        return v;
    }

    public static string Error(string message, Exception ex)
    {
        string logFrom = Assembly.GetCallingAssembly().GetName().Name;
        string time = DateTime.Now.ToString(TimeFormat);
        string v = $"{time} [Error] [{logFrom}] {message}\n{ex}";
        if (3 >= LogLevel)
        {
            LogWriter.WriteLine(v);
            Console.WriteLine(v);
        }
        return v;
    }

    public static string Fatal(string message, Exception ex)
    {
        string logFrom = Assembly.GetCallingAssembly().GetName().Name;
        string time = DateTime.Now.ToString(TimeFormat);
        string v = $"{time} [Fatal] [{logFrom}] {message}\n{ex}";
        if (4 >= LogLevel)
        {
            LogWriter.WriteLine(v);
            Console.WriteLine(v);
        }
        return v;
    }
}