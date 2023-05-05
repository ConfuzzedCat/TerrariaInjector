using Core;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;

[assembly: AssemblyTitle("TerrariaInjector")]
[assembly: AssemblyProduct("TerrariaInjector")]
[assembly: AssemblyCopyright("Copyright (c) 2023 / Confuzzedcat, #d1 & Co.")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.2.0")]
[assembly: AssemblyFileVersion("1.2.0")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: CLSCompliant(false)]
//[assembly: Guid("7A8659F1-61B8-4A3E-9201-000020230303")]
namespace TerrariaInjector
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.Title = "TerrariaInjector";
            AppDomain.CurrentDomain.AssemblyResolve += GM.DependencyResolveEventHandler;

            try
            {
                Logger.LogToConsole = true;
                Logger.Start();
                GM.Inject(args);
            }
            catch (Exception ex)
            {
                GM.Logger.Error("Fatal error!", ex);
            }
            finally
            {
                Logger.Shutdown();
                if (Logger.HasErrors)
                {
                    Console.WriteLine("\nAttention! Errors found, look into the logs ...");
                    GM.Wait();
                }

            }
        }
    }

    
    public static class GM
    {
        public static readonly string AssemblyFile = Assembly.GetExecutingAssembly().Location;
        public static readonly string AssemblyFolder = Path.GetFullPath(Path.GetDirectoryName(AssemblyFile) + Path.DirectorySeparatorChar);
        public static readonly Core.Logger Logger = new Core.Logger("GM");
        public static void Wait() => Console.ReadKey(true);
        public static readonly string[] Targets = { "Stardew Valley.exe", "Terraria.exe" };
        public static int ModCount = 0;

        public static void Inject(string[] args)
        {
            string modFolder = Path.Combine(AssemblyFolder, "Mods");
            string modDepsFolder = Path.Combine(modFolder, "Libs");
            if (!Directory.Exists(modFolder))
                Directory.CreateDirectory(modFolder);
            if (!Directory.Exists(modDepsFolder))
                Directory.CreateDirectory(modDepsFolder);


            string targetPath = null;
            var targets = new List<string>(Targets);
            string targetFile = Path.Combine(modFolder, "target");
            if (File.Exists(targetFile))
                targets.Insert(0, File.ReadAllText(targetFile));
            foreach (var entry in targets.Where(entry => !string.IsNullOrEmpty(entry)))
            {
                targetPath = Path.Combine(AssemblyFolder, entry);
                if (File.Exists(targetPath))
                    break;
            }
            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                throw new Exception($"Target assembly not found! {targetPath}");


            Logger.Info("Loading mods dependencies:");
            string[] modDependencies = Directory.GetFiles(modDepsFolder, "*.dll");
            foreach (var file in modDependencies)
            {
                Logger.Info("Loading: " + file);
                Assembly asm = Assembly.LoadFile(file);
                Logger.Debug("Found assembly: " + asm.ToString()); 
                Logger.Debug("AssemblyName: " + asm.GetName()); Logger.Debug("Found assembly: " + asm.ToString());
                AppDomain.CurrentDomain.Load(asm.GetName());
            }


            AssemblyDefinition gameAssemblyDef = null;
            var modsAssemblies = new List<Assembly>();
            Logger.Info("Loading mods:");
            foreach (var file in Directory.GetFiles(modFolder, "*.dll"))
            {
                Logger.Info("Loading: " + file);
                Assembly mod = Assembly.LoadFile(file);
                modsAssemblies.Add(mod);
                ModCount++;
                foreach (var type in mod.GetTypes())
                {
                    try
                    {
                        type.GetMethod("Init")?.Invoke(new object(), new object[] { });
                        type.GetMethod("Initialize")?.Invoke(new object(), new object[] { });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to invoke Initialize() method!", ex);
                    }
                    if (type.GetMethod("PrePatch") != null && gameAssemblyDef == null)
                    {
                        Logger.Info($"Loading game assembly definition: {targetPath}");
                        gameAssemblyDef = AssemblyDefinition.ReadAssembly(targetPath, new ReaderParameters() { ReadWrite = true, InMemory = true });
                    }
                    try
                    {
                        type.GetMethod("PrePatch")?.Invoke(new object(), new object[] { gameAssemblyDef });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to invoke PrePatch() method!", ex);
                    }
                }
            }


            Assembly game;
            Logger.Info($"Loading game assembly: {targetPath}");
            if (gameAssemblyDef == null)
                game = Assembly.LoadFile(targetPath); //game = Assembly.Load(File.ReadAllBytes(targetPath));
            else
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    gameAssemblyDef.Write(memoryStream); //gameAssemblyDef.Write(targetPath);
                    game = Assembly.Load(memoryStream.GetBuffer());
                }
            }
            bool isTerrariaTarget = false;

            if (targetPath.ToLower().EndsWith("terraria.exe") || File.Exists(Path.Combine(AssemblyFolder, "ReLogic.Native.dll")))
            {
                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
                game.GetType("Terraria.Program").GetField("SavePath").SetValue(null, savePath);
                isTerrariaTarget = true;
            }


            Logger.Info("Loading game dependencies ...");
            foreach (var file in game.GetManifestResourceNames())
                if (file.Contains(".dll"))
                {
                    Logger.Info("Loading: " + file);
                    Stream input = game.GetManifestResourceStream(file);
                    Assembly.Load(ReadStreamAssembly(input));
                }


            if (gameAssemblyDef != null)
                File.Move(targetPath, targetPath + ".bak");
            Harmony harmony = new Harmony("com.github.confuzzedcat.terraria.terrariainjector");
            foreach (var mod in modsAssemblies)
            {
                Logger.Info("Harmony.PatchAll() mod: " + mod.GetName().Name);
                try
                {
                    harmony.PatchAll(mod);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Harmony.PatchAll() failed on {mod.GetName().Name}!", ex);
                }
            }
            if (gameAssemblyDef != null)
                File.Move(targetPath + ".bak", targetPath);
            
            Logger.Debug("Assemblies:");
            Array.ForEach(AppDomain.CurrentDomain.GetAssemblies(), entry =>
            {
                Logger.Debug($"Loaded: {entry.FullName}");
                if (entry.FullName.IndexOf("terraria", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    Logger.Debug($"        {entry.CodeBase}");
            });


            foreach (var method in harmony.GetPatchedMethods())
                Logger.Info($"Patched method: \"{method.Name}\"");

            if (isTerrariaTarget)
            {
                try
                {
                    ModCountLabel.Patch(game, harmony);
                }
                catch (Exception ex)
                {
                    Logger.Error("ModcountLabel failed to patch!", ex);
                }
            }

            //Logger.Info("Writing patched game to file ...");
            //File.WriteAllBytes(targetPath + ".dump.exe", DumpAssembly(Game));

            Logger.Info("Invoke game entry point ...");
            Thread.Sleep(1000);
            game.EntryPoint.Invoke(null, new object[] { args });
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
                Logger.Error($"DumpAssembly() failed on {assembly.GetName().Name}!", ex);
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
            Logger.Warning($"Try loading: {args.Name}");

            // Thank you Rick (https://weblog.west-wind.com/posts/2016/dec/12/loading-net-assemblies-out-of-seperate-folders)... totally not yoinked from there...
            // check for assemblies already loaded
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
            {
                Logger.Warning($"Return loaded assembly: {args.Name}");
                return assembly;
            }

            // Try to load by filename - split out the filename of the full assembly name
            // and append the base path of the original assembly (ie. look in the same dir)
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            string asmFile = Path.Combine(@".\", filename);
            if (!File.Exists(asmFile))
                asmFile = Path.Combine(@".\", "Mods", filename);
            if (!File.Exists(asmFile))
                asmFile = Path.Combine(@".\", "Mods", "Libs", filename);
            if (!File.Exists(asmFile))
                return null;

            try
            {
                Logger.Info($"Loading assembly {asmFile}");
                return Assembly.LoadFrom(asmFile);
            }
            catch (Exception ex)
            {
                Logger.Error("Error trying to load Assembly", ex);
                return null;
            }
        }
    }
}
