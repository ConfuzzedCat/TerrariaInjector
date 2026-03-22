using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Applies Harmony patches from mod assemblies.
/// </summary>
public class HarmonyPatcher
{
    private readonly InjectionContext _context;

    public HarmonyPatcher(InjectionContext context)
    {
        _context = context;
    }

    public Harmony Patch(IEnumerable<Assembly> mods, Harmony customHarmonyInstance = null)
    {
        var harmony = customHarmonyInstance ?? _context.HarmonyInstance;

        foreach (var mod in mods)
        {
            try
            {
                _context.Logger.LogInformation($"Patching: {mod.GetName().Name}");
                harmony.PatchAll(mod);
            }
            catch (Exception ex)
            {
                _context.Logger.LogError("Patch failed", ex);
            }
        }
        return harmony;
    }
}