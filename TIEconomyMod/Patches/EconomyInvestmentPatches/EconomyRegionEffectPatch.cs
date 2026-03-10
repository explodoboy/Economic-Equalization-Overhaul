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
    [HarmonyPatch(typeof(TINationState), "OnEconomyPriorityComplete")]
    public static class EconomyRegionEffectPatch
    {
        // The base threshold is stored as its own variable to allow quick fixing if the devs change the hardcoded requirement.
        public static int baseOilThreshold = 500;
        public static int baseMiningThreshold = 750;
        public static int baseEconomicThreshold = 1200;

        // The region upgrade threshold is based on simulated IP.
        public static int OilThreshold => (Main.enabled) ? Math.Min(1, (int)(baseOilThreshold * Main.settings.regionUpgradeThresholdMult * Tools.IPFactor(CurrentNation))) : baseOilThreshold;
        public static int MiningThreshold => (Main.enabled) ? (int)(baseMiningThreshold * Main.settings.regionUpgradeThresholdMult * Tools.IPFactor(CurrentNation)) : baseMiningThreshold;
        public static int EconomicThreshold => (Main.enabled) ? (int)(baseEconomicThreshold * Main.settings.regionUpgradeThresholdMult * Tools.IPFactor(CurrentNation)) : baseEconomicThreshold;

        // This basically is a reference to the final threshold variables, which the post-transpiler code can call on.
        public static readonly MethodInfo getOilThreshold = AccessTools.PropertyGetter(typeof(EconomyRegionEffectPatch), nameof(OilThreshold));
        public static readonly MethodInfo getMiningThreshold = AccessTools.PropertyGetter(typeof(EconomyRegionEffectPatch), nameof(MiningThreshold));
        public static readonly MethodInfo getEconomicThreshold = AccessTools.PropertyGetter(typeof(EconomyRegionEffectPatch), nameof(EconomicThreshold));

        // Before the function runs, cache the current nation so an accurate tooltip may be displayed.
        private static TINationState CurrentNation => PriorityPatches.GetCurrentNation();
        [HarmonyPrefix]
        public static bool CacheNation(TINationState __instance)
        {
            PriorityPatches.SetCurrentNation(__instance);

            return true; // Skip the original getter.
        }

        // This replaces hardcoded variables, in this case I'm increasing the requirements by 5x due to all the extra IP.
        // This does NOT change the localization's reported requirement to add a special region. That has to be done separately.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // What this does, is that it replaces several hard-coded integers in priorityTipStr() with read-only variables.
            // Those read-only variables 
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == baseOilThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, getOilThreshold);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == baseMiningThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, getMiningThreshold);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == baseEconomicThreshold)
                {
                    yield return new CodeInstruction(OpCodes.Call, getEconomicThreshold);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}