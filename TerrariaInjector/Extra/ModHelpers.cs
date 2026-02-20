// Thanks to Tiberiumfusion, Confuzzedcat & Co.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Terraria;
using HarmonyLib;
using Microsoft.Xna.Framework.Input;

namespace TerrariaInjector
{   
    /// <summary>
    /// Contains various helper and convenience methods for HPlugins to use.
    /// </summary>

    public static class ModHelpers
    {
        #region Extensions
        
        public static CodeMatcher NopAndAdvance(this CodeMatcher matcher, int number)
        {
            while (number > 0)
            {
                matcher.SetAndAdvance(OpCodes.Nop, null);
                number--;
            }
            return matcher;
        }
        
        #endregion
        
        #region Misc helpers
        
        public static class Tools
        {       
            public static bool IsLocalPlayerFreeForAction()
            {
                if (Main.gameMenu || Main.ingameOptionsWindow || Main.playerInventory || Main.mapFullscreen || 
                        Main.editChest || Main.editSign || Main.autoPause || Main.LocalPlayer.talkNPC != -1)
                            return false;
                return true;
            }
            
            public static bool IsLocalPlayerTypingInChat()
            {
                return Main.drawingPlayerChat;
            }

            public static bool IsLocalPlayerRenamingChest()
            {
                return Main.editChest;
            }

            public static bool IsLocalPlayerEditingASign()
            {
                return Main.editSign;
            }

            public static bool IsLocalPlayerTypingInASearchBox()
            {
                return Main.CurrentInputTextTakerOverride != null;
            }
            
            public static bool IsLocalPlayerTypingSomething()
            {
                return Main.drawingPlayerChat || Main.editChest || Main.editSign || IsLocalPlayerTypingInASearchBox();
            }           
        }
    
        #endregion  

        #region Transpiler

        public static class Transpiler
        {
            /// <summary>
            /// Scans a method for a pattern of opcodes.
            /// </summary>
            public static int ScanForPattern(List<CodeInstruction> instructions, params OpCode[] pattern)
            {
                return ScanForPattern(instructions, (v, i) => true, 0, pattern);
            }

            /// <summary>
            /// Scans a method for a pattern of opcodes.
            /// </summary>
            public static int ScanForPattern(List<CodeInstruction> instructions, Func<int, CodeInstruction, bool> check,
                params OpCode[] pattern)
            {
                return ScanForPattern(instructions, check, 0, pattern);
            }
            
            /// <summary>
            /// Scans a method for a pattern of opcodes.
            /// </summary>
            public static int ScanForPattern(List<CodeInstruction> instructions, Func<int, CodeInstruction, bool> check,
                            int nStartOffset, params OpCode[] pattern)
            {
                for (var x = nStartOffset; x <= instructions.Count - pattern.Length; x++)
                {
                    if (instructions[x].opcode != pattern[0])
                        continue;
                    for (var y = 0; y < pattern.Length; y++)
                    {
                        if (instructions[x + y].opcode != pattern[y])
                            break;
                        if (y == pattern.Length - 1 && check(x, instructions[x]))
                            return x;
                    }
                }
                return -1;
            }

            /// <summary>
            /// Scans a method for a pattern and nop it.
            /// </summary>          
            public static bool ScanAndNop(List<CodeInstruction> instructions, int offsetStart, int numNops, 
                                            params OpCode[] pattern)
            {
                var index = ScanForPattern(instructions, pattern);
                if (index < 0) return false;

                int start = Math.Max(0, Math.Min(instructions.Count, index + offsetStart));
                int end = Math.Max(0, Math.Min(instructions.Count, index + offsetStart + numNops));
                
                for (var x = start; x < end; x++)
                {
                    instructions[x].opcode = OpCodes.Nop;
                    instructions[x].operand = null;
                }
                return true;
            }       
        }

        #endregion

        #region Input Reading Helpers
        
        /// <summary>
        /// Specifically contains helper methods for use in reading human input.
        /// </summary>
        public static class InputReading
        {
            /// <summary>
            /// Checks whether the specified key is currently held or not.
            /// </summary>
            /// <param name="key">The key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyDown(Keys key)
            {
                if (key == Keys.None) return false;
                return Terraria.Main.keyState.IsKeyDown(key);
            }

            /// <summary>
            /// Checks whether the specified key was down on this update cycle and up on the last update cycle.
            /// </summary>
            /// <param name="key">The key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyPressed(Keys key)
            {
                if (key == Keys.None) return false;
                return (Terraria.Main.keyState.IsKeyDown(key) && !Terraria.Main.oldKeyState.IsKeyDown(key));
            }
            
            /// <summary>
            /// Checks whether the base key was down on this update cycle and up on the last update cycle AND that the modifier key is currently held.
            /// If the modifier key is Keys.None, the modifier key down check will be skipped.
            /// </summary>
            /// <param name="baseKey">The base key to check.</param>
            /// <param name="modifierKey">The modifier key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyComboPressed(Keys baseKey, Keys modifierKey)
            {
                if (baseKey == Keys.None) return false;
                if (Terraria.Main.keyState.IsKeyDown(baseKey) && !Terraria.Main.oldKeyState.IsKeyDown(baseKey))
                {
                    if (modifierKey == Keys.None)
                        return true;
                    else
                        return (Terraria.Main.keyState.IsKeyDown(modifierKey));
                }
                else
                    return false;
            }

            /// <summary>
            /// Checks whether the base key AND the modifier key are both currently held.
            /// If the modifier key is Keys.None, the modifier key down check will be skipped.
            /// </summary>
            /// <param name="baseKey">The base key to check.</param>
            /// <param name="modifierKey">The modifier key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyComboDown(Keys baseKey, Keys modifierKey)
            {
                if (baseKey == Keys.None) return false;

                if (modifierKey == Keys.None)
                    return (Terraria.Main.keyState.IsKeyDown(baseKey));
                else
                    return (Terraria.Main.keyState.IsKeyDown(baseKey) && Terraria.Main.keyState.IsKeyDown(modifierKey));
            }
        }

        #endregion
    }
}
