using Microsoft.Extensions.DependencyInjection;
using TerrariaInjector.Core.Config;
using TerrariaInjector.Core.Injecting;
using TerrariaInjector.Core.Logging;
using TerrariaInjector.Extensions;

namespace TerrariaInjector.Core.DI;

public static class ServiceManager
{
    private static IServiceCollection _serviceCollection;
    public static IChildServiceCollection ChildServiceCollection { get; private set; }
    public static ServiceProvider ServiceProvider { get; private set; }
    public static ChildServiceProvider ChildServiceProvider { get; private set; }

    private static IServiceCollection CreateServiceCollection()
    {
        return new ServiceCollection()
            // config
            .AddSingleton<InjectorConfig>()

            // Loggers
            .AddSingleton<LoggerManager>()
            .AddKeyedLogger(nameof(Program))
            .AddKeyedLogger("Injector")
            .AddKeyedLogger("LifecycleHooks")

            // Injector services
            .AddSingleton<InjectionContext>()
            .AddSingleton<LifecycleHooks>()
            .AddTransient<AssemblyResolver>()
            .AddTransient<TargetResolver>()
            .AddTransient<DependencyLoader>()
            .AddTransient<ModLoader>()
            .AddTransient<GameLoader>()
            .AddTransient<HarmonyPatcher>()
            .AddTransient<Injector>();
    }
    internal static void BuildChildCollection()
    {
        ChildServiceProvider = (ChildServiceProvider)ChildServiceCollection.BuildChildServiceProvider(ServiceProvider);
    }
    
    internal static void Teardown()
    {
        ServiceProvider.Dispose();
        ChildServiceProvider.Dispose();
    }

    internal static void Startup()
    {
        _serviceCollection = CreateServiceCollection();
        ServiceProvider = _serviceCollection.BuildServiceProvider();
        ChildServiceCollection = _serviceCollection.CreateChildServiceCollection();
    }
}