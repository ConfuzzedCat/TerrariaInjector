using System;
using System.IO;
using System.Reflection;

namespace TerrariaInjector.Core.Injecting;

public static class TerrariaFix
{
    public static Assembly Fix(Assembly game)
    {
        string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
        var savePathField = game.GetType("Terraria.Program")?.GetField("SavePath");
        if (savePathField != null)
        {
            savePathField.SetValue(null, savePath);
        }

        return game;
    }
}