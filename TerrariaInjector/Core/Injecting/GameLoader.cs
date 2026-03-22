#nullable enable
using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Loads the target game assembly.
/// </summary>
public class GameLoader
{
    private readonly InjectionContext _context;

    public GameLoader(InjectionContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Loads the assembly definition into a memory assembly and loads it.
    /// </summary>
    /// <param name="targetPath">Target assembly path.</param>
    /// <param name="def">Target assembly definition</param>
    /// <returns>Memory assembly of the target.</returns>
    public Assembly Load(string targetPath, AssemblyDefinition? def)
    {
        _context.Logger.LogInformation($"Loading game assembly: {targetPath}");
        Assembly gameAsm;

        if (def == null)
        {
            gameAsm = Assembly.UnsafeLoadFrom(targetPath);
        }
        else
        {
            using var ms = new MemoryStream();
            def.Write(ms);
            gameAsm = Assembly.Load(ms.ToArray());
        }
        var targetLower = targetPath.ToLower();
        //TODO: unix-ify this.
        if (targetLower.EndsWith("terraria.exe") || targetLower.EndsWith("terrariaserver.exe"))
        {
            gameAsm = TerrariaFix.Fix(gameAsm);
            _context.GameTarget = "Terraria";
        }

        return gameAsm;
    }
}