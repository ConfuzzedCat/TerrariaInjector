using System;
using System.IO;
using System.Reflection;
using TerrariaInjector.Extensions;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Loads external dependency assemblies.
/// </summary>
public class DependencyLoader
{
    private readonly InjectionContext _context;

    public DependencyLoader(InjectionContext context)
    {
        _context = context;
    }
    
    public void Load()
    {
        if (!Directory.Exists(_context.DepsDir))
        {
            return;
        }

        _context.Logger.LogInformation("Loading dependencies...");

        foreach (var file in Directory.GetFiles(_context.DepsDir, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                _context.Logger.LogInformation($"Loading dependency: {file}");
                var asm = Assembly.UnsafeLoadFrom(file);
                AppDomain.CurrentDomain.Load(asm.GetName());

            }
            catch (BadImageFormatException e)
            {
                _context.Logger.LogError($"Dependency was made for the wrong version of dotnet. {file}", e);
            }
            catch (Exception e)
            {
                _context.Logger.LogError($"Failed to load dependency: {file}", e);
            }
        }
    }

    public void LoadGameDependencies(Assembly game)
    {
        _context.Logger.LogInformation("Loading game dependencies ...");
        game.LoadAssembliesFromManifestResourceStream(_context.Logger);
    }
}