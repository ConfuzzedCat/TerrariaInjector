using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace TerrariaInjector
{
    class Program
    {
        private static List<string> modDependencies { get; set; }
        public static Assembly game { get; set; }
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
                string injectorEXE = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty).Replace("/", "\\");
                if (args != null)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].ToLower().Contains("terraria"))
                        {
                            Logger.Info(args[i]);
                            terrariaExePath = args[i].Replace("\"", "");
                        }
                    }
                }
                if (!Directory.Exists(modFolder))
                {
                    Directory.CreateDirectory(modFolder);
                }
                List<string> mods = Directory.GetFiles(modFolder, "*.dll").OfType<string>().ToList();
                modDependencies = Directory.GetFiles(Path.Combine(modFolder, "Libs"), "*.dll").OfType<string>().ToList();
                game = Assembly.LoadFile(terrariaExePath);
                List<string> gameDependencies = new List<string>()
                {
                    "CsvHelper.dll",
                    "Ionic.Zip.CF.dll",
                    "MP3Sharp.dll",
                    "Newtonsoft.Json.dll",
                    "NVorbis.dll",
                    "RailSDK.Net.dll",
                    "ReLogic.dll",
                    "Steamworks.NET.dll",
                    "System.ValueTuple.dll",
                };
                if (injectorEXE.ToLower().Contains("uninstall") || injectorEXE.ToLower().Contains("remove"))
                {
                    foreach (var item in gameDependencies)
                    {
                        File.Delete(Path.Combine(terrariaPath, item));
                    }
                    Directory.Delete(modFolder, true);
                    Delete();
                }
                Logger.Info("Checking for dependencies...");
                List<string> existingDependencies = new List<string>();
                foreach (var item in gameDependencies)
                {
                    if (File.Exists(Path.Combine(terrariaPath, item)))
                    {
                        existingDependencies.Add(item);
                        Logger.Info("Found: " + item);
                    }
                }
                foreach (var item in existingDependencies)
                {
                    gameDependencies.Remove(item);
                }

                if (gameDependencies.Count >= 1)
                {
                    Logger.Info("Missing Game dependencies... Extracting from Terraria.exe!");
                    foreach (var file in game.GetManifestResourceNames())
                    {
                        string f = file;
                        foreach (string item in gameDependencies)
                        {
                            if (f.Contains(item))
                            {
                                Stream input = game.GetManifestResourceStream(f);
                                f = Rename(f);
                                using (Stream _file = File.Create(Path.Combine(terrariaPath, f)))
                                {
                                    CopyStream(input, _file);
                                }
                                Logger.Info("Extracted: " + f);
                            }
                        }
                    }
                }
                else
                {
                    Logger.Info("All dependencies found...");
                }
                if (!(modDependencies is null))
                {
                    foreach (var file in modDependencies)
                    {
                        AppDomain.CurrentDomain.Load(Assembly.LoadFile(Path.GetFullPath(file)).GetName());
                    }
                }


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
                object[] gameParam = new object[] { args, false };

                //AppDomain.CurrentDomain.ExecuteAssemblyByName(game.GetName());
                game.GetType("Terraria.Program").GetMethod("LaunchGame").Invoke(new object(), gameParam);
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
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }
        private static string Rename(string filename)
        {
            switch (filename)
            {
                case "Terraria.Libraries.ReLogic.ReLogic.dll":
                    return "ReLogic.dll";

                case "Terraria.Libraries.DotNetZip.Ionic.Zip.CF.dll":
                    return "Ionic.Zip.CF.dll";

                case "Terraria.Libraries.JSON.NET.Newtonsoft.Json.dll":
                    return "Newtonsoft.Json.dll";

                case "Terraria.Libraries.CsvHelper.CsvHelper.dll":
                    return "CsvHelper.dll";

                case "Terraria.Libraries.NVorbis.NVorbis.dll":
                    return "NVorbis.dll";

                case "Terraria.Libraries.NVorbis.System.ValueTuple.dll":
                    return "System.ValueTuple.dll";

                case "Terraria.Libraries.MP3Sharp.MP3Sharp.dll":
                    return "MP3Sharp.dll";

                case "Terraria.Libraries.Steamworks.NET.Windows.Steamworks.NET.dll":
                    return "Steamworks.NET.dll";

                case "Terraria.Libraries.RailSDK.Windows.RailSDK.Net.dll":
                    return "RailSDK.Net.dll";
                default:
                    return "";
            }
        }
        private static void Delete()
        {
            string batchCommands = string.Empty;
            string exeFileName = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty).Replace("/", "\\");
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
            string asmFile = Path.Combine(@".\", "Mods", "Libs", filename);
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
    }
    
    [HarmonyPatch]
    public class MainMenuPatch
    {
        public static MethodBase TargetMethod()
        {
            return Program.game.GetType("Terraria.Main").GetMethod("DrawVersionNumber", BindingFlags.NonPublic | BindingFlags.Static);
        }
        public static void Prefix()
        {
            Program.game.GetType("Terraria.Main").GetField("versionNumber").SetValue(null, "v1.4.3.6 - Injected");
        }
    }
    
}
