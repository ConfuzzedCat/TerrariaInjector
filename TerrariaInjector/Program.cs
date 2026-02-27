using System;
using System.ComponentModel.Design;
using System.IO;
using TerrariaInjector.Core.Config;
using TerrariaInjector.Core.Injecting;
using TerrariaInjector.Core.Logging;
using TerrariaInjector.Extensions;


namespace TerrariaInjector
{
    public static class Program
    {
        public const string VERSION = "2.0.0";
        
        public static ServiceContainer ServiceContainer { get; private set; }
        private static ILogger _logger;

        private static ServiceContainer CreateServiceContainer()
        {
            return new ServiceContainer()
                    .AddLogger<ILogger>()
                    .AddConfig<InjectorConfig>();
        }
        public static void Wait() => Console.ReadKey(true);
        
        [STAThread]
        public static void Main(string[] args)
        {
            ServiceContainer = CreateServiceContainer();
            Console.Title = "TerrariaInjector";
            AppDomain.CurrentDomain.AssemblyResolve += Injector.DependencyResolveEventHandler;
            try
            {
                // Load config early to determine log directory
                var config = ServiceContainer.GetRequiredService<InjectorConfig>();
                string logDir = string.Empty;
                if (!string.IsNullOrEmpty(config.LogsFolder))
                {
                    logDir = Path.Combine(Injector.AssemblyFolder, config.RootFolder, config.LogsFolder);
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                }

                LoggerOptions options = new LoggerOptions()
                {
                    LogFile = new FileInfo("Crash-%date%.log")
                };
                
                _logger = ServiceContainer.GetLoggerService(nameof(Program), options);
                Injector.Inject(args);
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error!", ex);
            }
            finally
            {
                if (_logger.HasErrors)
                {
                    Console.WriteLine("\nAttention! Errors found, look into the logs ...");
                    try
                    {
                    }
                    catch
                    {
                        Wait();
                    }
                }
                Teardown();
            }
        }

        private static void Teardown(object sender = null, EventArgs e = null)
        {
            ServiceContainer.Dispose();
        }
    }
}
