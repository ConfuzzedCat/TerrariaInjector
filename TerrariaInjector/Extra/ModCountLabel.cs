using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using TerrariaInjector.Core.DI;
using TerrariaInjector.Core.Logging;
using TerrariaInjector.Extensions;

namespace TerrariaInjector.Extra
{
    public class ModCountLabel
    {
        private static Assembly _game;
        private static int _modCount;
        public static void Patch(Assembly terraria, Harmony harmony, int modCount)
        {
            _modCount = modCount;
            _game = terraria;
            harmony.Patch(TargetMethod(), new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => ModCountLabel.Prefix())));
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

            if (!version.Contains("Modded"))
            {
                version += $" - Modded({_modCount})!";
                _game.GetType("Terraria.Main").GetField("versionNumber").SetValue(null, version);
            }
        }
    }
}
