using System;
using HarmonyLib;
using Terraria;
using Terraria.DataStructures;

namespace TerrariaInjector
{
    [HarmonyPatch]
    class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.Hurt))]
        static void PreHurt(PlayerDeathReason damageSource, int Damage, int hitDirection, bool pvp = false, bool quiet = false, bool Crit = false, int cooldownCounter = -1)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Ran PreHurt.");
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.LoadPlayer))]
        static void PreLoadPlayer(string playerPath, bool cloudSave)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Ran PreLoadPlayer.");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnHit))]
        static void PreOnHit(float x, float y, Entity victim)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Ran PreOnHit.");
        }
    }
}
