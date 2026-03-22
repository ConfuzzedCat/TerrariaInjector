using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Resolves the target executable to inject into.
/// </summary>
public class TargetResolver
{
    private readonly InjectionContext _context;

    private static readonly string[] DefaultTargets =
    {
        "Terraria.exe",
        "TerrariaServer.exe",
        "Stardew Valley.exe",
        // Linux targets
        "Terraria",
        "TerrariaServer",
        "Stardew Valley"
    };

    public TargetResolver(InjectionContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Resolves the target game to inject into, using either defaults or a target file.
    /// The target most be in the same directory as injector.
    /// </summary>
    /// <returns>The full path of the target assembly</returns>
    /// <exception cref="FileNotFoundException">Throws if the target cant be found or is invalid</exception>
    public string Resolve()
    {
        var targets = new List<string>(DefaultTargets);

        string targetFile = Path.Combine(_context.RootDir, "target");
        if (File.Exists(targetFile))
        {
            targets.Insert(0, File.ReadAllText(targetFile).Trim());
        }

        foreach (var entry in targets.Where(x => !string.IsNullOrEmpty(x)))
        {
            string path = Path.Combine(_context.AssemblyFolder, entry);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("Target assembly not found.");
    }
}