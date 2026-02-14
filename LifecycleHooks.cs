using Core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TerrariaInjector
{
    /// <summary>
    /// Registers Harmony patches on Terraria's XNA lifecycle methods and dispatches
    /// callbacks to mod assemblies that define matching static methods.
    ///
    /// Mods can opt in by defining any of these public static void methods:
    ///   OnGameReady()      — Main.Initialize() postfix (graphics, window, Main.instance ready)
    ///   OnContentLoaded()  — Main.LoadContent() postfix (SpriteBatch, content pipeline ready)
    ///   OnFirstUpdate()    — First Main.Update() postfix (game loop active)
    ///   OnShutdown()       — Main_Exiting prefix (before Terraria disposes systems)
    /// </summary>
    public static class LifecycleHooks
    {
        private static readonly Logger Logger = new Logger("Lifecycle");

        private static readonly List<MethodInfo> _onGameReady = new List<MethodInfo>();
        private static readonly List<MethodInfo> _onContentLoaded = new List<MethodInfo>();
        private static readonly List<MethodInfo> _onFirstUpdate = new List<MethodInfo>();
        private static readonly List<MethodInfo> _onShutdown = new List<MethodInfo>();

        private static bool _gameReadyFired;
        private static bool _contentLoadedFired;
        private static bool _firstUpdateFired;
        private static bool _shutdownFired;

        /// <summary>
        /// Discover lifecycle methods in mod assemblies and register Harmony patches
        /// on Terraria's Main class to dispatch them at the right time.
        /// </summary>
        public static void Register(Assembly game, Harmony harmony, List<Assembly> mods)
        {
            DiscoverMethods(mods);

            int total = _onGameReady.Count + _onContentLoaded.Count +
                        _onFirstUpdate.Count + _onShutdown.Count;
            Logger.Info($"Registering hooks ({total} method(s) found across {mods.Count} mod(s))");

            var mainType = game.GetType("Terraria.Main");
            if (mainType == null)
            {
                Logger.Error("Terraria.Main not found — lifecycle hooks not registered");
                return;
            }

            // OnGameReady — Main.Initialize() postfix
            PatchMethod(harmony, mainType, "Initialize",
                BindingFlags.NonPublic | BindingFlags.Instance,
                nameof(OnGameReady_Postfix), patchType: "postfix");

            // OnContentLoaded — Main.LoadContent() postfix
            PatchMethod(harmony, mainType, "LoadContent",
                BindingFlags.NonPublic | BindingFlags.Instance,
                nameof(OnContentLoaded_Postfix), patchType: "postfix");

            // OnFirstUpdate — Main.Update(GameTime) postfix
            PatchMethod(harmony, mainType, "Update",
                BindingFlags.NonPublic | BindingFlags.Instance,
                nameof(OnFirstUpdate_Postfix), patchType: "postfix");

            // OnShutdown — Main.Main_Exiting(object, EventArgs) prefix
            PatchMethod(harmony, mainType, "Main_Exiting",
                BindingFlags.NonPublic | BindingFlags.Instance,
                nameof(OnShutdown_Prefix), patchType: "prefix");
        }

        private static void PatchMethod(Harmony harmony, Type targetType, string methodName,
            BindingFlags flags, string callbackName, string patchType)
        {
            var target = targetType.GetMethod(methodName, flags);
            if (target == null)
            {
                Logger.Warning($"Method not found: Main.{methodName} — hook skipped");
                return;
            }

            var callback = typeof(LifecycleHooks).GetMethod(callbackName,
                BindingFlags.Public | BindingFlags.Static);

            switch (patchType)
            {
                case "prefix":
                    harmony.Patch(target, prefix: new HarmonyMethod(callback));
                    break;
                case "postfix":
                    harmony.Patch(target, postfix: new HarmonyMethod(callback));
                    break;
                default:
                    Logger.Warning($"Unknown patch type '{patchType}' for Main.{methodName} — hook skipped");
                    return;
            }

            Logger.Info($"Hooked Main.{methodName} ({patchType})");
        }

        private static void DiscoverMethods(List<Assembly> mods)
        {
            var hookMap = new Dictionary<string, List<MethodInfo>>
            {
                { "OnGameReady", _onGameReady },
                { "OnContentLoaded", _onContentLoaded },
                { "OnFirstUpdate", _onFirstUpdate },
                { "OnShutdown", _onShutdown },
            };

            foreach (var mod in mods)
            {
                Type[] types;
                try
                {
                    types = mod.GetTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    types = rtle.Types;
                }

                foreach (var type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    foreach (var kvp in hookMap)
                    {
                        var method = type.GetMethod(kvp.Key,
                            BindingFlags.Public | BindingFlags.Static,
                            null, Type.EmptyTypes, null);

                        if (method != null)
                        {
                            kvp.Value.Add(method);
                            Logger.Info($"Found {kvp.Key}() in {type.FullName}");
                        }
                    }
                }
            }
        }

        // --- Harmony callbacks ---

        public static void OnGameReady_Postfix()
        {
            if (_gameReadyFired)
            {
                return;
            }
            _gameReadyFired = true;
            Logger.Info("OnGameReady fired");
            Dispatch(_onGameReady, "OnGameReady");
        }

        public static void OnContentLoaded_Postfix()
        {
            if (_contentLoadedFired)
            {
                return;
            }
            _contentLoadedFired = true;
            Logger.Info("OnContentLoaded fired");
            Dispatch(_onContentLoaded, "OnContentLoaded");
        }

        public static void OnFirstUpdate_Postfix()
        {
            if (_firstUpdateFired)
            {
                return;
            }
            _firstUpdateFired = true;
            Logger.Info("OnFirstUpdate fired");
            Dispatch(_onFirstUpdate, "OnFirstUpdate");
        }

        public static void OnShutdown_Prefix()
        {
            if (_shutdownFired)
            {
                return;
            }
            _shutdownFired = true;
            Logger.Info("OnShutdown fired");
            Dispatch(_onShutdown, "OnShutdown");
        }

        private static void Dispatch(List<MethodInfo> methods, string hookName)
        {
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    var inner = ex.InnerException ?? ex;
                    Logger.Error($"{hookName} failed in {method.DeclaringType?.FullName}: {inner.Message}", inner);
                }
            }
        }
    }
}
