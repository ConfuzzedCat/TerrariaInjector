using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using TerrariaInjector.Core.Config;
using TerrariaInjector.Core.Logging;

namespace TerrariaInjector.Core.Injecting;

/// <summary>
/// Holds runtime state and configuration for the injection process.
/// </summary>
public class InjectionContext
{
    /// <summary>
    /// The folder where the injector (program) is running from. 
    /// </summary>
    public string AssemblyFolder { get; }
    /// <summary>
    /// The absolute path of the RootDir from the config (ini) file.
    /// </summary>
    public string RootDir { get; }
    /// <summary>
    /// The absolute path of the CoreDir from the config (ini) file.
    /// </summary>
    public string CoreDir { get; }
    /// <summary>
    /// The absolute path of the DepsDir from the config (ini) file.
    /// </summary>
    public string DepsDir { get; }
    /// <summary>
    /// The absolute path of the ModsDir from the config (ini) file.
    /// </summary>
    public string ModsDir { get; }
    /// <summary>
    /// The instance of the injector config.
    /// </summary>
    public InjectorConfig Config { get; }
    /// <summary>
    /// The harmony instance of the injector.
    /// </summary>
    public Harmony HarmonyInstance { get; }
    /// <summary>
    /// The logger instance of thr injector
    /// </summary>
    public ILogger Logger { get; }
    /// <summary>
    /// A string containing the target game for injection.
    /// </summary>
    public string GameTarget { get; internal set; }
    
    internal int ModsCount { get; set; }

    public InjectionContext(InjectorConfig config, [FromKeyedServices("Injector")]ILogger logger)
    {

        HarmonyInstance = new Harmony(Constants.HARMONY_INSTANCE);
        Config = config;
        Logger = logger;

        AssemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if(AssemblyFolder == null){
            throw new NullReferenceException("Invalid path for assembly folder. Where is Injector running from?");
        }
        AssemblyFolder += Path.DirectorySeparatorChar;

        RootDir = Path.Combine(AssemblyFolder, config.RootFolder);
        CoreDir = string.IsNullOrEmpty(config.CoreFolder) ? 
            RootDir
            : Path.Combine(RootDir, config.CoreFolder);

        DepsDir = Path.Combine(RootDir, config.DepsFolder);
        ModsDir = string.IsNullOrEmpty(config.ModsFolder)
            ? RootDir
            : Path.Combine(RootDir, config.ModsFolder);
    }
}