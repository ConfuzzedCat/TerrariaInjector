using System;
using System.ComponentModel.Design;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using TerrariaInjector.Core.Config;
using TerrariaInjector.Core.DI;
using TerrariaInjector.Core.Injecting;
using TerrariaInjector.Core.Logging;
//using TerrariaInjector.DependencyInjection;
using TerrariaInjector.Extensions;


namespace TerrariaInjector
{
    public static class Program
    {
        public const string VERSION = "2.0.0";

        private static ILogger _logger;

        public static void Wait() => Console.ReadKey(true);
        
        [STAThread]
        public static void Main(string[] args)
        {
            ServiceManager.Startup();
            Console.Title = "TerrariaInjector";
            try
            {
                // Load config early to determine log directory
                using var scope = ServiceManager.ServiceProvider.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<InjectorConfig>();
                var context = scope.ServiceProvider.GetRequiredService<InjectionContext>();
                string logDir = string.Empty;
                if (!string.IsNullOrEmpty(config.LogsFolder))
                {
                    logDir = Path.Combine(context.AssemblyFolder, config.RootFolder, config.LogsFolder);
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                }

                LoggerOptions options = new LoggerOptions()
                {
                    LogFile = new FileInfo("Crash-%date%.log")
                };

                _logger = scope.ServiceProvider.GetRequiredKeyedService<ILogger>(nameof(Program));
                var injector = scope.ServiceProvider.GetRequiredService<Injector>();
                injector.Run(args);
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
                ServiceManager.Teardown();
            }
        }
    }
}
