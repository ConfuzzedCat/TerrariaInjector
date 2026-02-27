using System.IO;

namespace TerrariaInjector.Core.Config;

public class InjectorConfig
{
    // [Paths]
    public string RootFolder { get; set; } = "Mods";
    public string CoreFolder { get; set; } = "";
    public string DepsFolder { get; set; } = "Libs";
    public string ModsFolder { get; set; } = "";
    public string LogsFolder { get; set; } = "";


    public InjectorConfig(string path)
    {
        string path1 = Path.Combine(path, "TerrariaModder", "core", "config.ini");
        string path2 = Path.Combine(path, "Mods", "config.ini");

        foreach (var p in new[] { path1, path2 })
        {
            if (!File.Exists(p))
            {
                continue;
            }

            try
            {
                ParseIni(File.ReadAllLines(p));
            }
            catch
            {
                // Fall through to defaults if parse fails
            }
        }
    }

    public InjectorConfig() : this(string.Empty)
    {
            
    }

    private void ParseIni(string[] lines)
    {
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines, comments, and section headers
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["))
            {
                continue;
            }

            var eqIndex = line.IndexOf('=');
            if (eqIndex <= 0)
            {
                continue;
            }

            var key = line.Substring(0, eqIndex).Trim().ToLowerInvariant();
            var value = line.Substring(eqIndex + 1).Trim();

            switch (key)
            {
                case "rootfolder":
                    this.RootFolder = value;
                    break;
                case "corefolder":
                    this.CoreFolder = value;
                    break;
                case "depsfolder":
                    this.DepsFolder = value;
                    break;
                case "modsfolder":
                    this.ModsFolder = value;
                    break;
                case "logsfolder":
                    this.LogsFolder = value;
                    break;
            }
        }
    }
}