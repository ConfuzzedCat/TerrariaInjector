using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using TerrariaInjector;

using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using Terraria.Audio;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace D1.Plugins
{
	[HarmonyPatch]
	public class template
	{
		[HarmonyPatch(typeof(Player), "AddBuff")]
		public static void Prefix(int type, ref int timeToAdd)		
		{
			if (BuffID.Sets.IsAFlaskBuff[type] || type == 159 || type == 29 || type == 150|| type == 93)
				timeToAdd *= 5;
		}
	
		[HarmonyPatch(typeof(ShopHelper), "GetShoppingSettings")]		
		public static void Postfix(ShopHelper __instance, ShoppingSettings __result)
		{	
			Traverse.Create(__instance).Field("_currentPriceAdjustment").SetValue(1);
			__result.PriceAdjustment = 1;			
		}
	
		[HarmonyPatch(typeof(TeleportPylonsSystem), "TeleportPylons")]
		public static void Postfix(ref int __result)
		{	
			__result = 0;
			return false;
		}
	
		[HarmonyPatch(typeof(Main), "DoUpdateInWorld")]		
		public static void Postfix()
		{	
			if (!ModHelpers.InputReading.IsKeyComboPressed(Keys.S, Keys.LeftAlt)
			if (!ModHelpers.InputReading.IsKeyPressed(Keys.T) || !ModHelpers.Tools.IsLocalPlayerFreeForAction())
				return;

			Player player = Main.player[Main.myPlayer];
		}

		// https://github.com/BepInEx/HarmonyX/wiki/Transpiler-helpers
		// https://gist.github.com/JavidPack/454477b67db8b017cb101371a8c49a1c
		[HarmonyPatch(typeof(Projectile), "FishingCheck")]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(TeleportPylonsSystem), "HandleTeleportRequest")]
		static IEnumerable<CodeInstruction> HandleTeleportRequest(IEnumerable<CodeInstruction> instructions)
		{
			GM.DumpCodeInstructions("./mods/transpiler-before", instructions);
			var result = new CodeMatcher(instructions)
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldloc_2),
					new CodeMatch(OpCodes.Ldstr, "Net.CannotTeleportToPylonBecauseAccessingLihzahrdTempleEarly"))
					new CodeMatch(i => i.opcode == OpCodes.Ldsfld  && ((FieldInfo)i.operand).Name == "worldSurface"),
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(NPC), "AnyDanger")),
				.ThrowIfInvalid("HandleTeleportRequest(): Error! Can't find pattern!")
				.Advance(-4)
				.SetOpcodeAndAdvance(OpCodes.Nop)
				.SetAndAdvance(OpCodes.Nop, null)
				.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Main), "AnglerQuestSwap"))
				.NopAndAdvance(23)				
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldc_I4_1),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Foo), "Foo")),					
				.RemoveInstructions(8)				
				.InstructionEnumeration();
				
			GM.DumpCodeInstructions("./mods/transpiler-after", result);			
			return result;
		}
	}
}
