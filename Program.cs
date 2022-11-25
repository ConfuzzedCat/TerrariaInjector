using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TerrariaInjector
{
    class Program
    {
        internal static Assembly game;
        internal static int modCount;
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "TerrariaInjector";
                Logger.Start();
                AppDomain.CurrentDomain.AssemblyResolve += DependencyResolveEventHandler;

                string terrariaPath = Directory.GetCurrentDirectory();
                string terrariaExePath = Path.Combine(terrariaPath, "Terraria.exe");
                string modFolder = Path.Combine(terrariaPath, "Mods");
                string modDepsFolder = Path.Combine(modFolder, "Libs");
                string injectorEXE = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty).Replace("/", "\\");

                if (!File.Exists(terrariaExePath))
                {
                    throw new Exception("Terraria.exe Not Found!");
                }

                if (!Directory.Exists(modFolder))
                {
                    Directory.CreateDirectory(modFolder);
                }
                if (!Directory.Exists(modDepsFolder))
                {
                    Directory.CreateDirectory(modDepsFolder);
                }

                Delete(modFolder);

                string[] mods = Directory.GetFiles(modFolder, "*.dll");
                modCount = mods.Length;
                string[] modDependencies = Directory.GetFiles(modDepsFolder, "*.dll");

                game = Assembly.LoadFile(terrariaExePath);

                Logger.Info("Loading Game dependencies");



                foreach (var file in game.GetManifestResourceNames())
                {
                    if (file.Contains(".dll"))
                    {
                        Stream input = game.GetManifestResourceStream(file);
                        Assembly.Load(ReadStreamAssembly(input));
                        Logger.Info("Loaded: " + file);
                    }
                }

                if (!(modDependencies is null))
                {
                    foreach (var file in modDependencies)
                    {
                        AppDomain.CurrentDomain.Load(Assembly.LoadFile(Path.GetFullPath(file)).GetName());
                    }
                }

                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
                if (!Directory.Exists(savePath))
                {
                    Exception e = new DirectoryNotFoundException(savePath);
                    Logger.Fatal("Default save folder doesn't exist.", e);
                    throw e;
                }
                game.GetType("Terraria.Program").GetField("SavePath").SetValue(null, savePath);
                Logger.Info("Initializing patching...");
                Harmony harmony = new Harmony("com.github.confuzzedcat.terraria.terrariainjector");
                foreach (var mod in mods)
                {
                    Assembly _mod = Assembly.LoadFile(mod);
                    Logger.Info("Mod loaded: " + _mod.GetName());
                    harmony.PatchAll(_mod);
                }
                foreach (var method in harmony.GetPatchedMethods())
                {
                    Logger.Info($"Patch method: \"{method.Name}\"");
                }
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                object[] gameParam = new object[] { args };
                MethodInfo entryMethod = game.EntryPoint;

                entryMethod.Invoke(null, gameParam);
            }
            catch (Exception e)
            {
                Logger.Fatal("Fatal error!", e);
            }
            finally
            {
                Logger.Info("Closing...");
                Logger.Stop();
                System.Threading.Thread.Sleep(2500);
            }
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
        private static void Delete(string modFolder)
        {
            string exeFileName = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty).Replace("/", "\\");
            if (exeFileName.ToLower().Contains("uninstall") || exeFileName.ToLower().Contains("remove"))
            {
                Directory.Delete(modFolder, true);

                string batchCommands = string.Empty;
                string injectorDependency = Path.Combine(Directory.GetCurrentDirectory(), "0Harmony.dll");


                batchCommands += "@ECHO OFF\n";                         // Do not show any output
                batchCommands += "ping 127.0.0.1 > nul\n";              // Wait approximately 4 seconds (so that the process is already terminated)
                batchCommands += "echo j | del /F ";                    // Delete the executeable
                batchCommands += exeFileName + "\n";
                batchCommands += "echo j | del /F ";                    // Delete the executeable
                batchCommands += injectorDependency + "\n";
                batchCommands += "echo j | del TerrariaInjectorUninstaller.bat";    // Delete this bat file

                File.WriteAllText("TerrariaInjectorUninstaller.bat", batchCommands);

                Process.Start("TerrariaInjectorUninstaller.bat");
                Environment.Exit(0);
            }
        }
        private static Assembly DependencyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            // Thank you Rick (https://weblog.west-wind.com/posts/2016/dec/12/loading-net-assemblies-out-of-seperate-folders)... totally not yoinked from there...

            // check for assemblies already loaded
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            // Try to load by filename - split out the filename of the full assembly name
            // and append the base path of the original assembly (ie. look in the same dir)
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            //string asmFile = Path.Combine(@".\", "Mods", "Libs", filename);
            string asmFile = Path.Combine(@".\", filename);
            try
            {
                return Assembly.LoadFrom(asmFile);
            }
            catch (Exception e)
            {
                Logger.Error("Error trying to load Assembly", e);
                return null;
            }
        }
        private static Assembly TDependencyResolveEventHandler(object sender, ResolveEventArgs sargs)
        {
            string resourceName = new AssemblyName(sargs.Name).Name + ".dll";
            string text = Array.Find<string>(typeof(Program).Assembly.GetManifestResourceNames(), (string element) => element.EndsWith(resourceName));
            if (text == null)
            {
                return null;
            }
            Assembly result;
            using (Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(text))
            {
                byte[] array = new byte[manifestResourceStream.Length];
                manifestResourceStream.Read(array, 0, array.Length);
                result = Assembly.Load(array);
            }
            return result;
        }
    }

    [HarmonyPatch]
    class MainMenuPatch
    {
        static MethodBase TargetMethod()
        {
            return Program.game.GetType("Terraria.Main").GetMethod("DrawVersionNumber", BindingFlags.NonPublic | BindingFlags.Static);
        }
        static void Prefix()
        {
            string add = $" - Mod count:";
            string version = (string)Program.game.GetType("Terraria.Main").GetField("versionNumber").GetValue(null);
            if (!version.Contains(add))
            {
                version += add + " " + Program.modCount;
            }
            Program.game.GetType("Terraria.Main").GetField("versionNumber").SetValue(null, version);
        }
    }
}
