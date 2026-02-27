using System.Reflection;
using HarmonyLib;

namespace TerrariaInjector.Core.Injecting
{
    public class ModCountLabel
    {
        public static void Patch(Assembly Terraria, Harmony harmony)
        {
            _game = Terraria;
            harmony.Patch(TargetMethod(), new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => ModCountLabel.Prefix())));
        }
        private static Assembly _game;

        public static void Initialize()
        {
            Injector.Logger.LogInformation(MethodBase.GetCurrentMethod().DeclaringType.Name + " initialized!");
        }
        static MethodBase TargetMethod()
        {
            var mainType = _game.GetType("Terraria.Main");
            var method = mainType.GetMethod("DrawVersionNumber", BindingFlags.NonPublic | BindingFlags.Static);
            return method;
        }
        static void Prefix()
        {
            string version = (string)_game.GetType("Terraria.Main").GetField("versionNumber").GetValue(null);

            if (!version.Contains("Modded!"))
            {
                version += " - Modded! (" + Injector.ModCount + ")";
                _game.GetType("Terraria.Main").GetField("versionNumber").SetValue(null, version);
            }
        }
    }
}
