using System;
using System.ComponentModel.Design;
using System.Reflection;
using TerrariaInjector.Core.Logging;
using TerrariaInjector.Utils;

namespace TerrariaInjector.Extensions;

public static class ServiceExtensions
{
    extension(ServiceContainer container)
    {
        public ServiceContainer AddService<TService, TImplementation>() where TImplementation : class, TService
        {
            container.AddService(typeof(TService), CreateInstance<TImplementation>());
            return container;
        }

        public ServiceContainer AddLogger<TService>(LoggerOptions options = null)
        {
            if (LoggerImpl.Instance != null &&  LoggerImpl.Instance.Started)
            {
                return container;
            }
            LoggerImpl.CreateInstance(options);
            container.AddService(typeof(TService), LoggerImpl.Instance);
            return container;
        }

        public ServiceContainer AddLogger<TService>(Func<LoggerOptions> optionsFactory)
        {
            return container.AddLogger<TService>(optionsFactory.Invoke());
        }

        public ServiceContainer AddConfig<TConfig>(string path = "") where TConfig : class
        {
            TConfig config;
            if (string.IsNullOrWhiteSpace(path))
            {
                config = CreateInstance<TConfig>([
                    Assembly.GetExecutingAssembly().Location
                ]);
            }
            else
            {
                config = CreateInstance<TConfig>([path]);
            }
            container.AddService(typeof(TConfig), config);
            return container;
        }
    }

    extension(IServiceProvider provider)
    {
        public TService GetRequiredService<TService>() where TService : class
        {
            var service = provider.GetService(typeof(TService)) as TService;
            Guard.ThrowIfNull(service);
            return service;
        }

        public ILogger GetLoggerService(string name, LoggerOptions options = null, EventHandler<LogInfoArgs> logMessageAdded = null)
        {
            var logger = provider.GetRequiredService<ILogger>() as LoggerImpl;
            if (options == null)
            {
                options = LoggerOptions.Default;
            }
            if (string.IsNullOrWhiteSpace(name) == false)
            {
                options.LoggerName = name;
            }
            return new Logger(logger, options, logMessageAdded);
        }

        public ILogger GetLoggerService<T>(LoggerOptions options = null, EventHandler<LogInfoArgs> logMessageAdded = null)
        {
            return provider.GetLoggerService(typeof(T).Name, options, logMessageAdded);
        }
        
        public ILogger GetLoggerService(string name, Func<LoggerOptions> optionsFactory)
        {
            return provider.GetLoggerService(name, optionsFactory.Invoke());
        }

        public ILogger GetLoggerService<T>(Func<LoggerOptions> optionsFactory)
        {
            return provider.GetLoggerService(typeof(T).Name, optionsFactory);
        }
    }


    private static TService CreateInstance<TService>() where TService : class
    {
        return Activator.CreateInstance<TService>();
    }
    private static TService CreateInstance<TService>(params object[] args) where TService : class
    {
        return (TService)Activator.CreateInstance(typeof(TService), args);
    }
}