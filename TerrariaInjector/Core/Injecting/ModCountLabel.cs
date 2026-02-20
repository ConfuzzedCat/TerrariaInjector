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
			GM.Logger.LogInformation(MethodBase.GetCurrentMethod().DeclaringType.Name + " initialized!");
		}
		static MethodBase TargetMethod()
		{
			return _game.GetType("Terraria.Main").GetMethod("DrawVersionNumber", BindingFlags.NonPublic | BindingFlags.Static);
		}
		static void Prefix()
		{
			string version = (string)_game.GetType("Terraria.Main").GetField("versionNumber").GetValue(null);

			if (!version.Contains("Modded!"))
			{
				version += " - Modded! (" + GM.ModCount + ")";
				_game.GetType("Terraria.Main").GetField("versionNumber").SetValue(null, version);
			}
		}
	}
}
