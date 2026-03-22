using Microsoft.Extensions.DependencyInjection;

namespace TerrariaInjector.Core.DI;
/// <summary>
/// Inherent this class if you have services to registered, such as loggers.
/// </summary>
public interface IServiceInfo
{
    public void RegisterServices(IChildServiceCollection  serviceCollection);
}