using System;
using Microsoft.Extensions.DependencyInjection;
using TerrariaInjector.Core.Config;
using TerrariaInjector.Core.Logging;

namespace TerrariaInjector.Extensions;

public static class LoggerServiceExtensions
{
    public static IServiceCollection AddKeyedLogger(this IServiceCollection services, string key)
    {
        services.AddKeyedLogger(key, LoggerOptions.Default);
        
        return services;
    }
    public static IServiceCollection AddKeyedLogger(this IServiceCollection services, string key, LoggerOptions options)
    {
        services.AddKeyedLogger(key, options, null);
        
        return services;
    }
    public static IServiceCollection AddKeyedLogger(this IServiceCollection services, string key, LoggerOptions options, EventHandler<LogInfoArgs> logMessageAdded)
    {
        services.AddKeyedSingleton<ILogger, Logger>(key, (provider, o) =>
        {
            using var scope = provider.CreateScope();

            var logMan = scope.ServiceProvider.GetRequiredService<LoggerManager>();

            return new Logger(logMan, options, logMessageAdded);
        });
        
        return services;
    }
    
}