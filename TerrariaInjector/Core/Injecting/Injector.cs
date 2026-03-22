using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using TerrariaInjector.Core.DI;
using TerrariaInjector.Extra;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Main orchestrator for the injection pipeline.
/// </summary>
public class Injector
{
    private readonly InjectionContext _context;
    private readonly TargetResolver _targetResolver;
    private readonly DependencyLoader _dependencyLoader;
    private readonly ModLoader _modLoader;
    private readonly GameLoader _gameLoader;
    private readonly HarmonyPatcher _patcher;
    private readonly AssemblyResolver _assemblyResolver;
    private readonly LifecycleHooks _lifecycleHooks;

    public Injector(
        InjectionContext context,
        TargetResolver targetResolver,
        DependencyLoader dependencyLoader,
        ModLoader modLoader,
        GameLoader gameLoader,
        HarmonyPatcher patcher,
        AssemblyResolver assemblyResolver,
        LifecycleHooks lifecycleHooks
        )
    {
        _context = context;
        _targetResolver = targetResolver;
        _dependencyLoader = dependencyLoader;
        _modLoader = modLoader;
        _gameLoader = gameLoader;
        _patcher = patcher;
        _assemblyResolver = assemblyResolver;
        _lifecycleHooks = lifecycleHooks;
    }

    /// <summary>
    /// Executes the full injection pipeline.
    /// </summary>
    public void Run(string[] args)
    {
        _assemblyResolver.Register();
        
        _dependencyLoader.Load();

        string targetPath = _targetResolver.Resolve();

        AssemblyDefinition gameAssemblyDef = AssemblyDefinition.ReadAssembly(
            targetPath,
            new ReaderParameters { ReadWrite = true, InMemory = true }
        );
        var mods = _modLoader.LoadMods(ref gameAssemblyDef, targetPath);
        _context.ModsCount = mods.Count;

        var game = _gameLoader.Load(targetPath, gameAssemblyDef);

        
        _dependencyLoader.LoadGameDependencies(game);

        var harmony = _patcher.Patch(mods);
        if (_context.GameTarget == "Terraria")
        {
            ModCountLabel.Patch(game, harmony, _context.ModsCount);
        }
        
        if (IsTerrariaClient(targetPath))
        {
            _lifecycleHooks.Register(game, harmony, mods);
        }

        _context.Logger.LogInformation("Starting game...");
        Thread.Sleep(1000);

        game.EntryPoint.Invoke(null, [args]);
    }
    
    // TODO: make compability with linux-native
    private bool IsTerrariaClient(string targetPath)
    {
        var lower = targetPath.ToLower();
        return lower.Contains("terraria.exe") && !lower.Contains("server");
    }
}