using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TINationState), "OnWelfarePriorityComplete")]
    public static class WelfareRegionEffectPatch
    {
        // Refer to EconomyRegionEffectPatch.cs for specifics on everything that's happening here.

        public static int baseDecolonizeThreshold = 1000;
        public static int DecolonizeThreshold => (Main.enabled) ? (int)(baseDecolonizeThreshold * Main.settings.regionUpgradeThresholdMult * Tools.IPFactor(CurrentNation)) : baseDecolonizeThreshold;
        public static readonly MethodInfo getDecolonizeThreshold = AccessTools.PropertyGetter(typeof(WelfareRegionEffectPatch), nameof(DecolonizeThreshold));

        private static TINationState CurrentNation => PriorityPatches.GetCurrentNation();
        [HarmonyPrefix]
        public static bool CacheNation(TINationState __instance)
        {
            PriorityPatches.SetCurrentNation(__instance);

            return true; // Skip the original getter.
        }

        // Replaces hardcoded variables in OnWelfarePriorityComplete(). The tooltip's stated threshold has to be updated separately.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Refer to PriorityTooltipPatch.cs for details.
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == baseDecolonizeThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, getDecolonizeThreshold);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}