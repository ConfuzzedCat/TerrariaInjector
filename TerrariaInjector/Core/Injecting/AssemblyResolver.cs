using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Resolves missing assemblies at runtime by searching known directories.
/// </summary>
public class AssemblyResolver
{
    private readonly InjectionContext _context;
    private readonly Dictionary<string, Assembly> _cache = new();

    public AssemblyResolver(InjectionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registers the resolver with the current AppDomain.
    /// </summary>
    public void Register()
    {
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }

    /// <summary>
    /// Handles assembly resolution when the runtime fails to locate a dependency.
    /// </summary>
    private Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            if (_cache.TryGetValue(args.Name, out var cached))
            {
                return cached;
            }
            
            // Check if assembly is already loaded in Current Appdomain, if so return it.
            var loaded = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.FullName == args.Name);

            if (loaded != null)
            {
                return loaded;
            }

            // Get the assembly filename.
            string filename = args.Name.Split(',')[0] + ".dll";

            // Build search paths and search for assembly.
            var searchPaths = BuildSearchPaths();

            foreach (var path in searchPaths)
            {
                if (!Directory.Exists(path))
                {
                    continue;
                }

                var candidate = Path.Combine(path, filename);

                if (!File.Exists(candidate))
                {
                    continue;
                }

                _context.Logger.LogDebug($"Resolving assembly: {candidate}");

                var foundAssembly = Assembly.UnsafeLoadFrom(candidate);
                _cache.Add(args.Name, foundAssembly);
                return foundAssembly;
            }
        }
        catch (Exception ex)
        {
            _context.Logger.LogError("Assembly resolution failed", ex);
        }

        return null;
    }

    /// <summary>
    /// Builds all directories to search for assemblies.
    /// </summary>
    private IEnumerable<string> BuildSearchPaths()
    {
        var paths = new List<string>
        {
            _context.AssemblyFolder,
            _context.RootDir,
            _context.DepsDir,
            _context.ModsDir,

            // fallback legacy paths
            Path.Combine(_context.AssemblyFolder, "Mods"),
            Path.Combine(_context.AssemblyFolder, "Mods", "Libs")
        };

        return paths.Distinct();
    }
}