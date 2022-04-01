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
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "TerrariaInjector";
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
                            Console.WriteLine(args[i]); 
                            terrariaExePath = args[i].Replace("\"","");
                        }
                    }
                }
                if (!Directory.Exists(modFolder))
                {
                    Directory.CreateDirectory(modFolder);
                }
                List<string> mods = Directory.GetFiles(modFolder, "*.dll").OfType<string>().ToList();
                Assembly game = Assembly.LoadFile(terrariaExePath);
                string injectorDependency = Path.Combine(terrariaPath, "0Harmony.dll");
                if (!File.Exists(injectorDependency))
                {
                    throw new Exception("Missing file: 0Harmony.dll");
                }
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
                    //File.Delete(injectorDependency);
                    Delete();
                }
                Console.WriteLine("Checking for dependencies...");
                List<string> existingDependencies = new List<string>();
                foreach (var item in gameDependencies)
                {
                    if (File.Exists(Path.Combine(terrariaPath,item)))
                    {
                        existingDependencies.Add(item);
                        Console.WriteLine("Found: "+ item);
                    }
                }
                foreach (var item in existingDependencies)
                {
                    gameDependencies.Remove(item);
                }
                
                if (gameDependencies.Count >= 1)
                { 
                    Console.WriteLine("Missing Game dependencies... Extracting from Terraria.exe!");
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
                                Console.WriteLine("Extracted: " + f);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("All dependencies found...");
                }

                Console.WriteLine("Initializing patching...");
                Harmony harmony = new Harmony("com.github.confuzzedcat.terraria.terrariainjector");
                foreach (var mod in mods)
                {
                    harmony.PatchAll(Assembly.LoadFrom(mod));
                    Console.WriteLine("Mod loaded: " +mod);
                }
                foreach (var method in harmony.GetPatchedMethods())
                {
                    Console.WriteLine($"Patch method: \"{method.Name}\"");
                }
                object[] gameParam = new object[] { args, false };
                game.GetType("Terraria.Program").GetMethod("LaunchGame").Invoke(new object(), gameParam);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }
            finally
            {
                Console.WriteLine("Closing...");
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
    }
}
