using System;
using System.ComponentModel.Design;
using TerrariaInjector.Core.Logging;
using TerrariaInjector.Utils;

namespace TerrariaInjector.Extensions;

public static class ServiceExtensions
{
    extension(ServiceContainer container)
    {
        public ServiceContainer AddService<TService, TImplementation>()
        {
            container.AddService(typeof(TService), CreateInstance<TImplementation>());
            return container;
        }

        public ServiceContainer AddLogger<TService>(LoggerOptions options = null)
        {
            LoggerImpl.CreateInstance(options);
            container.AddService(typeof(TService), LoggerImpl.Instance);
            return container;
        }

        public ServiceContainer AddLogger<TService>(Func<LoggerOptions> optionsFactory)
        {
            LoggerImpl.CreateInstance(optionsFactory.Invoke());
            container.AddService(typeof(TService), LoggerImpl.Instance);
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

        public ILogger GetLoggerService(string name, LoggerOptions options = null)
        {
            var logger = provider.GetRequiredService<ILogger>() as LoggerImpl;
            if (options == null)
            {
                options = LoggerOptions.Default;
                options.LoggerName = name;
            }

            return new Logger(logger, options);
        }

        public ILogger GetLoggerService<T>(LoggerOptions options = null)
        {
            return provider.GetLoggerService(typeof(T).Name, options);
        }
        
        public ILogger GetLoggerService(string name, Func<LoggerOptions> optionsFactory)
        {
            var logger = provider.GetRequiredService<ILogger>() as LoggerImpl;

            return new Logger(logger, optionsFactory.Invoke());
        }

        public ILogger GetLoggerService<T>(Func<LoggerOptions> optionsFactory)
        {
            return provider.GetLoggerService(typeof(T).Name, optionsFactory);
        }
    }


    private static TService CreateInstance<TService>()
    {
        return Activator.CreateInstance<TService>();
    }
}