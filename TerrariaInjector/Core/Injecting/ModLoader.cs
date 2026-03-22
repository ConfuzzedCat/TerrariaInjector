using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using TerrariaInjector.Core.DI;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Loads mod assemblies and invokes lifecycle hooks.
/// </summary>
public class ModLoader
{
    private readonly InjectionContext _context;

    public ModLoader(InjectionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Finds and loads mods into the target.
    /// </summary>
    /// <param name="gameAssemblyDef">Target assembly</param>
    /// <param name="targetPath"></param>
    /// <param name="quiet">Whether to log exceptions when invoking methods in the mod.</param>
    /// <returns></returns>
    public List<Assembly> LoadMods(ref AssemblyDefinition gameAssemblyDef, string targetPath, bool quiet = true)
    {
        var result = new List<Assembly>();

        var modPaths = new List<string>();

        if (Directory.Exists(_context.CoreDir))
        {
            modPaths.AddRange(
                Directory.GetFiles(_context.CoreDir, "*.dll")
            );
        }

        if (_context.ModsDir != _context.CoreDir && Directory.Exists(_context.ModsDir))
        {
            modPaths.AddRange(
                Directory.GetFiles(_context.ModsDir, "*.dll", SearchOption.AllDirectories)
            );
        }

        foreach (var file in modPaths)
        {
            _context.Logger.LogInformation($"Registering services for mod: {file}");
            var mod = Assembly.UnsafeLoadFrom(file);
            foreach (var type in mod.GetTypes())
            {
                if (typeof(IServiceInfo).IsAssignableFrom(type)&&
                    !type.IsAbstract &&
                    !type.IsInterface)
                {
                    var instance = (IServiceInfo)Activator.CreateInstance(type);
                    instance.RegisterServices(ServiceManager.ChildServiceCollection);
                    break;
                }
            }
        }
        
        ServiceManager.BuildChildCollection();

        foreach (var file in modPaths)
        {
            _context.Logger.LogInformation($"Loading mod: {file}");

            var mod = Assembly.UnsafeLoadFrom(file);
            result.Add(mod);
            

            foreach (var type in mod.GetTypes())
            {
                InvokeOptional(quiet, type, "Init");
                InvokeOptional(quiet, type, "Initialize");

                if (type.GetMethod("PrePatch") != null && gameAssemblyDef == null)
                {
                    gameAssemblyDef = AssemblyDefinition.ReadAssembly(
                        targetPath,
                        new ReaderParameters { ReadWrite = true, InMemory = true }
                    );
                }

                InvokeOptional(quiet, type, "PrePatch", gameAssemblyDef);
            }
        }

        return result;
    }

    private void InvokeOptional(bool quiet, Type type, string method, params object[] args)
    {
        try
        {
            type.GetMethod(method)?.Invoke(Activator.CreateInstance(type), args);
        }
        catch (Exception e)
        {
            if (quiet == false)
            {
                _context.Logger.LogWarning($"Failed to invoke method: {method}", e);
            }
        }
    }
}