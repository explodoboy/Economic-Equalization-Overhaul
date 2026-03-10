using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TIEconomyMod
{
    [HarmonyPatch(typeof(TINationState), "OnEnvironmentPriorityComplete")]
    public static class EnvironmentRegionEffectPatch
    {
        // Refer to EconomyRegionEffectPatch.cs for specifics on everything that's happening here.

        public static int baseCleanupThreshold = 100;
        public static int CleanupThreshold => (Main.enabled) ? (int)(baseCleanupThreshold * Main.settings.regionUpgradeThresholdMult * Tools.IPFactor(CurrentNation)) : baseCleanupThreshold;
        public static readonly MethodInfo getCleanupThreshold = AccessTools.PropertyGetter(typeof(EnvironmentRegionEffectPatch), nameof(CleanupThreshold));

        private static TINationState CurrentNation => PriorityPatches.GetCurrentNation();
        [HarmonyPrefix]
        public static bool CacheNation(TINationState __instance)
        {
            PriorityPatches.SetCurrentNation(__instance);

            return true; // Skip the original getter.
        }

        // Replaces hardcoded variables in OnEnvironmentPriorityComplete(). The tooltip's stated threshold has to be updated separately.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Refer to PriorityTooltipPatch.cs for details.
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == baseCleanupThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, getCleanupThreshold);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}