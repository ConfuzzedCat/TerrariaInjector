using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Mono.Cecil;
using TerrariaInjector.Core.Config;
using TerrariaInjector.Core.Logging;
using TerrariaInjector.Extensions;

namespace TerrariaInjector.Core.Injecting;

public static class Injector
{
    public static readonly string AssemblyFile = Assembly.GetExecutingAssembly().Location;
    public static readonly string AssemblyFolder = Path.GetFullPath(Path.GetDirectoryName(AssemblyFile) + Path.DirectorySeparatorChar);
    public static ILogger Logger;
    public static readonly string[] Targets = { "Stardew Valley.exe", "Terraria.exe", "TerrariaServer.exe" };
    public static int ModCount = 0;
    public static InjectorConfig Config;
    public static string RootDir;
    public static string CoreDir;
    public static string DepsDir;
    public static string ModsDir;
    

    public static void Inject(string[] args)
    {
            
        Initialization();


        string targetPath = null;
        var targets = new List<string>(Targets);
        string targetFile = Path.Combine(RootDir, "target");
        if (File.Exists(targetFile))
        {
            targets.Insert(0, File.ReadAllText(targetFile).Trim());
        }
        foreach (var entry in targets.Where(entry => !string.IsNullOrEmpty(entry)))
        {
            targetPath = Path.Combine(AssemblyFolder, entry);
            if (File.Exists(targetPath))
            {
                break;
            }
        }
        if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
        {
            throw new Exception($"Target assembly not found! {targetPath}");
        }

        bool isServer = targetPath.ToLower().EndsWith("terrariaserver.exe");
        Logger.LogInformation($"Target: {targetPath} (Server mode: {isServer})");


        Logger.LogInformation("Loading dependencies from: " + DepsDir);
        if (Directory.Exists(DepsDir))
        {
            foreach (var file in Directory.GetFiles(DepsDir, "*.dll", SearchOption.AllDirectories))
            {
                Logger.LogInformation($"Loading dependency: {file}");
                Assembly asm = Assembly.UnsafeLoadFrom(file);
                Logger.LogDebug($"Found assembly: {asm}");
                AppDomain.CurrentDomain.Load(asm.GetName());
            }
        }


        AssemblyDefinition gameAssemblyDef = null;
        var modsAssemblies = new List<Assembly>();

        var modPaths = new List<string>();
        if (Directory.Exists(CoreDir))
        {
            modPaths.AddRange(Directory.GetFiles(CoreDir, "*.dll", SearchOption.TopDirectoryOnly));
        }
        if (ModsDir != CoreDir && Directory.Exists(ModsDir))
        {
            modPaths.AddRange(Directory.GetFiles(ModsDir, "*.dll", SearchOption.AllDirectories));
        }

        Logger.LogInformation("Loading mods:");
        foreach (var file in modPaths)
        {
            Logger.LogInformation("Loading: " + file);
            Assembly mod = Assembly.UnsafeLoadFrom(file);
            modsAssemblies.Add(mod);
            ModCount++;
            foreach (var type in mod.GetTypes())
            {
                try
                {
                    type.GetMethod("Init")?.Invoke(new object(), new object[] { });
                    type.GetMethod("Initialize")?.Invoke(new object(), new object[] { });
                }
                catch
                {
                    // Expected for mods that don't use Init/Initialize pattern
                }
                if (type.GetMethod("PrePatch") != null && gameAssemblyDef == null)
                {
                    Logger.LogInformation($"Loading game assembly definition: {targetPath}");
                    gameAssemblyDef = AssemblyDefinition.ReadAssembly(targetPath, new ReaderParameters() { ReadWrite = true, InMemory = true });
                }
                try
                {
                    type.GetMethod("PrePatch")?.Invoke(new object(), new object[] { gameAssemblyDef });
                }
                catch
                {
                    // Expected for mods that don't use PrePatch pattern
                }
            }
        }


        Assembly game;
        Logger.LogInformation($"Loading game assembly: {targetPath}");
        if (gameAssemblyDef == null)
        {
            game = Assembly.UnsafeLoadFrom(targetPath);
        }
        else
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                gameAssemblyDef.Write(memoryStream); //gameAssemblyDef.Write(targetPath);
                game = Assembly.Load(memoryStream.GetBuffer());
            }
        }
        bool isTerrariaTarget = false;
        string targetLower = targetPath.ToLower();

        if (targetLower.EndsWith("terraria.exe") || targetLower.EndsWith("terrariaserver.exe") || File.Exists(Path.Combine(AssemblyFolder, "ReLogic.Native.dll")))
        {
            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
            var savePathField = game.GetType("Terraria.Program")?.GetField("SavePath");
            if (savePathField != null)
            {
                savePathField.SetValue(null, savePath);
            }
            isTerrariaTarget = true;
        }


        Logger.LogInformation("Loading game dependencies ...");
        foreach (var file in game.GetManifestResourceNames())
        {
            if (file.Contains(".dll"))
            {
                Logger.LogInformation("Loading: " + file);
                Stream input = game.GetManifestResourceStream(file);
                Assembly.Load(ReadStreamAssembly(input));
            }
        }


        if (gameAssemblyDef != null)
        {
            File.Move(targetPath, targetPath + ".bak");
        }
        Harmony harmony = new Harmony("com.github.confuzzedcat.terraria.terrariainjector");
        foreach (var mod in modsAssemblies)
        {
            Logger.LogInformation("Harmony.PatchAll() mod: " + mod.GetName().Name);
            try
            {
                harmony.PatchAll(mod);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Harmony.PatchAll() failed on {mod.GetName().Name}!", ex);
            }
        }
        if (gameAssemblyDef != null)
        {
            File.Move(targetPath + ".bak", targetPath);
        }
            
        Logger.LogDebug("Assemblies:");
        Array.ForEach(AppDomain.CurrentDomain.GetAssemblies(), entry =>
        {
            Logger.LogDebug($"Loaded: {entry.FullName}");
            if (entry.FullName.IndexOf("terraria", StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                Logger.LogDebug($"\t{entry.CodeBase}");
            }
        });


        foreach (var method in harmony.GetPatchedMethods())
            Logger.LogInformation($"Patched method: \"{method.Name}\"");

        if (isTerrariaTarget && !isServer)
        {
            try
            {
                ModCountLabel.Patch(game, harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError("ModcountLabel failed to patch!", ex);
            }
        }

        // Register lifecycle hooks (Terraria client only)
        if (isTerrariaTarget && !isServer)
        {
            LifecycleHooks.Register(game, harmony, modsAssemblies);
        }

        Logger.LogInformation("Invoke game entry point ...");
        Thread.Sleep(1000);
        game.EntryPoint.Invoke(null, [args]);
    }

    private static void Initialization()
    {
        Logger = Program.ServiceContainer.GetLoggerService("GM", () => new LoggerOptions()
        {
            LogFile = new FileInfo("%date%_GM.log"),
        });
        Config = Program.ServiceContainer.GetRequiredService<InjectorConfig>();
        RootDir = Path.Combine(AssemblyFolder, Config.RootFolder);
        CoreDir = string.IsNullOrEmpty(Config.CoreFolder)
            ? RootDir
            : Path.Combine(RootDir, Config.CoreFolder);
        DepsDir = Path.Combine(RootDir, Config.DepsFolder);
        ModsDir = string.IsNullOrEmpty(Config.ModsFolder)
            ? RootDir
            : Path.Combine(RootDir, Config.ModsFolder);

        if (!Directory.Exists(RootDir))
        {
            Directory.CreateDirectory(RootDir);
        }
        if (!Directory.Exists(DepsDir))
        {
            Directory.CreateDirectory(DepsDir);
        }
        if (!string.IsNullOrEmpty(Config.ModsFolder) && !Directory.Exists(ModsDir))
        {
            Directory.CreateDirectory(ModsDir);
        }
    }

    public static byte[] DumpAssembly(Assembly assembly)
    {
        try
        {
            MethodInfo asmGetRawBytes = assembly.GetType().GetMethod("GetRawBytes", BindingFlags.Instance | BindingFlags.NonPublic);
            object bytesObject = asmGetRawBytes.Invoke(assembly, null);
            return (byte[])bytesObject;
        }
        catch (Exception ex)
        {
            Logger.LogError($"DumpAssembly() failed on {assembly.GetName().Name}!", ex);
            return null;
        }
    }

    public static void DumpCodeInstructions(string fileName, IEnumerable<CodeInstruction> instructions)
    {
        var text = new List<string>();
        int index = 0;
        foreach (var entry in instructions)
        {
            var line = index.ToString("0000") + ":    " + entry.ToString();
            if (line.EndsWith(" NULL"))
                line = line.Substring(0, line.LastIndexOf(" NULL"));
            text.Add(line.Trim());
            index++;
        }
        File.WriteAllLines(fileName, text);
    }

    private static byte[] ReadStreamAssembly(Stream assemblyStream)
    {
        byte[] array = new byte[assemblyStream.Length];
        using (Stream a = assemblyStream)
        {
            a.Read(array, 0, array.Length);
        }
        return array;
    }

    internal static Assembly DependencyResolveEventHandler(object sender, ResolveEventArgs args)
    {
        // check for assemblies already loaded
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
        if (assembly != null)
        {
            return assembly;
        }

        // Try to load by filename - split out the filename of the full assembly name
        // and append the base path of the original assembly (ie. look in the same dir)
        string filename = args.Name.Split(',')[0] + ".dll".ToLower();

        var searchPaths = new List<string> { AssemblyFolder };

        // Load config on-demand if not yet loaded
        if (Config == null)
        {
            try
            {
                Config = Program.ServiceContainer.GetRequiredService<InjectorConfig>();
            }
            catch(Exception e)
            {
                Logger.LogError("There was an error trying to load the config.", e);
            }
        }

        if (Config != null)
        {
            string rootDir = Path.Combine(AssemblyFolder, Config.RootFolder);
            string depsDir = Path.Combine(rootDir, Config.DepsFolder);
            string modsDir = string.IsNullOrEmpty(Config.ModsFolder) ? rootDir : Path.Combine(rootDir, Config.ModsFolder);

            searchPaths.Add(rootDir);
            searchPaths.Add(depsDir);
            if (modsDir != rootDir)
            {
                searchPaths.Add(modsDir);
            }
        }

        // Always include original paths as fallback
        searchPaths.Add(Path.Combine(AssemblyFolder, "Mods"));
        searchPaths.Add(Path.Combine(AssemblyFolder, "Mods", "Libs"));

        string asmFile = null;
        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
            {
                continue;
            }

            var candidate = Path.Combine(searchPath, filename);
            if (File.Exists(candidate))
            {
                asmFile = candidate;
                break;
            }
        }

        if (asmFile == null)
        {
            return null;
        }

        try
        {
            return Assembly.UnsafeLoadFrom(asmFile);
        }
        catch
        {
            return null;
        }
    }
}